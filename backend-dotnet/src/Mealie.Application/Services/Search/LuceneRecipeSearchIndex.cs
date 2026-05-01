using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Mealie.Application.Contracts.Search;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Directory = System.IO.Directory;

namespace Mealie.Application.Services.Search;

/// <summary>
///     Singleton Lucene.NET search index for recipes. Provides near-real-time search.
/// </summary>
public sealed class LuceneRecipeSearchIndex : IRecipeSearchIndex, IIndexDiagnostics, IDisposable
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;

    private readonly StandardAnalyzer _analyzer;
    private readonly ILogger<LuceneRecipeSearchIndex> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SearcherManager _searcherManager;
    private readonly IndexWriter _writer;

    public LuceneRecipeSearchIndex(
        IOptions<AppSettings> appSettings,
        IServiceScopeFactory scopeFactory,
        ILogger<LuceneRecipeSearchIndex> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var indexPath = Path.Combine(appSettings.Value.DataDir, "search", "recipes");
        Directory.CreateDirectory(indexPath);

        var directory = FSDirectory.Open(new DirectoryInfo(indexPath));
        _analyzer = new StandardAnalyzer(Version);
        var config = new IndexWriterConfig(Version, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };
        _writer = new IndexWriter(directory, config);
        _searcherManager = new SearcherManager(_writer, true, null);
    }

    public void Dispose()
    {
        _searcherManager.Dispose();
        _writer.Dispose();
        _analyzer.Dispose();
    }

    // ── IIndexDiagnostics ────────────────────────────────────────────────────

    public string Name => "recipes";

    public int GetDocumentCount()
    {
        var searcher = _searcherManager.Acquire();
        try
        {
            return searcher.IndexReader.NumDocs;
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>> RawSearch(string? query, int maxResults = 50)
    {
        _searcherManager.MaybeRefresh();
        var searcher = _searcherManager.Acquire();
        try
        {
            Query q;
            if (string.IsNullOrWhiteSpace(query))
            {
                q = new MatchAllDocsQuery();
            }
            else
            {
                var parser = new MultiFieldQueryParser(
                    Version,
                    ["name", "description", "tags", "categories"],
                    _analyzer);
                parser.DefaultOperator = Operator.OR;
                q = parser.Parse(QueryParserBase.Escape(query.Trim()));
            }

            var topDocs = searcher.Search(q, maxResults);
            var results = new List<IReadOnlyDictionary<string, string>>(topDocs.ScoreDocs.Length);

            foreach (var sd in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(sd.Doc);
                var dict = new Dictionary<string, string>();
                foreach (var fieldName in new[] { "id", "householdId", "slug", "name" })
                {
                    var val = doc.Get(fieldName);
                    if (val is not null)
                    {
                        dict[fieldName] = val;
                    }
                }

                results.Add(dict);
            }

            return results;
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public Task DeleteAsync(CancellationToken ct = default)
    {
        _writer.DeleteAll();
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        return Task.CompletedTask;
    }

    public async Task IndexAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Id == id)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            _logger.LogWarning("Recipe {RecipeId} not found for indexing", id);
            return;
        }

        var doc = new Document();
        doc.Add(new StringField("id", recipe.Id.ToString(), Field.Store.YES));
        doc.Add(new StringField("householdId", recipe.HouseholdId.ToString(), Field.Store.YES));
        doc.Add(new StringField("slug", recipe.Slug, Field.Store.YES));
        doc.Add(new TextField("name", recipe.Name, Field.Store.YES));
        doc.Add(new StringField("name_sort", recipe.Name.ToLowerInvariant(), Field.Store.NO));

        if (!string.IsNullOrWhiteSpace(recipe.Description))
        {
            doc.Add(new TextField("description", recipe.Description, Field.Store.NO));
        }

        var tagsText = string.Join(" ", recipe.Tags.Select(t => t.Name));
        doc.Add(new TextField("tags", tagsText, Field.Store.NO));

        var categoriesText = string.Join(" ", recipe.Categories.Select(c => c.Name));
        doc.Add(new TextField("categories", categoriesText, Field.Store.NO));

        // Also store slugs for exact filtering
        foreach (var tag in recipe.Tags)
        {
            doc.Add(new StringField("tag_slug", tag.Slug, Field.Store.NO));
        }

        foreach (var cat in recipe.Categories)
        {
            doc.Add(new StringField("cat_slug", cat.Slug, Field.Store.NO));
        }

        _writer.UpdateDocument(new Term("id", recipe.Id.ToString()), doc);
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();

        _logger.LogDebug("Indexed recipe {RecipeId} ({Slug})", id, recipe.Slug);
    }

    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _writer.DeleteDocuments(new Term("id", id.ToString()));
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        _logger.LogDebug("Removed recipe {RecipeId} from index", id);
        return Task.CompletedTask;
    }

    public Task<RecipeSearchResult> SearchAsync(RecipeSearchQuery query, CancellationToken ct = default)
    {
        _searcherManager.MaybeRefresh();
        var searcher = _searcherManager.Acquire();
        try
        {
            var boolQuery = new BooleanQuery();
            boolQuery.Add(
                new TermQuery(new Term("householdId", query.HouseholdId.ToString())),
                Occur.MUST);

            if (!string.IsNullOrWhiteSpace(query.Text))
            {
                try
                {
                    var parser = new MultiFieldQueryParser(
                        Version,
                        ["name", "description", "tags", "categories"],
                        _analyzer);
                    parser.DefaultOperator = Operator.OR;
                    var escaped = QueryParserBase.Escape(query.Text.Trim());
                    var textQuery = parser.Parse(escaped);
                    boolQuery.Add(textQuery, Occur.MUST);
                }
                catch (ParseException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse search query '{Text}'", query.Text);
                }
            }

            if (query.Tags is { Count: > 0 })
            {
                foreach (var tag in query.Tags)
                {
                    // Try exact slug match first
                    var tagTerm = tag.Contains(' ')
                        ? new TermQuery(new Term("tags", tag.ToLowerInvariant()))
                        : new TermQuery(new Term("tag_slug", tag.ToLowerInvariant()));
                    boolQuery.Add(tagTerm, Occur.MUST);
                }
            }

            if (query.Categories is { Count: > 0 })
            {
                foreach (var cat in query.Categories)
                {
                    var catTerm = cat.Contains(' ')
                        ? new TermQuery(new Term("categories", cat.ToLowerInvariant()))
                        : new TermQuery(new Term("cat_slug", cat.ToLowerInvariant()));
                    boolQuery.Add(catTerm, Occur.MUST);
                }
            }

            var sort = new Sort(new SortField("name_sort", SortFieldType.STRING));
            var maxDocs = query.Skip + query.Take;
            if (maxDocs <= 0)
            {
                maxDocs = 20;
            }

            var topDocs = searcher.Search(boolQuery, maxDocs, sort);
            var total = topDocs.TotalHits;

            var slugs = topDocs.ScoreDocs
                .Skip(query.Skip)
                .Select(sd => searcher.Doc(sd.Doc).Get("slug"))
                .Where(s => s is not null)
                .ToList();

            return Task.FromResult(new RecipeSearchResult(slugs!, total));
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public async Task RebuildAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding recipe search index...");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var recipes = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .ToListAsync(ct);

        _writer.DeleteAll();

        foreach (var recipe in recipes)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var doc = new Document();
            doc.Add(new StringField("id", recipe.Id.ToString(), Field.Store.YES));
            doc.Add(new StringField("householdId", recipe.HouseholdId.ToString(), Field.Store.YES));
            doc.Add(new StringField("slug", recipe.Slug, Field.Store.YES));
            doc.Add(new TextField("name", recipe.Name, Field.Store.YES));
            doc.Add(new StringField("name_sort", recipe.Name.ToLowerInvariant(), Field.Store.NO));

            if (!string.IsNullOrWhiteSpace(recipe.Description))
            {
                doc.Add(new TextField("description", recipe.Description, Field.Store.NO));
            }

            var tagsText = string.Join(" ", recipe.Tags.Select(t => t.Name));
            doc.Add(new TextField("tags", tagsText, Field.Store.NO));

            var categoriesText = string.Join(" ", recipe.Categories.Select(c => c.Name));
            doc.Add(new TextField("categories", categoriesText, Field.Store.NO));

            foreach (var tag in recipe.Tags)
            {
                doc.Add(new StringField("tag_slug", tag.Slug, Field.Store.NO));
            }

            foreach (var cat in recipe.Categories)
            {
                doc.Add(new StringField("cat_slug", cat.Slug, Field.Store.NO));
            }

            _writer.AddDocument(doc);
        }

        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();

        _logger.LogInformation("Recipe search index rebuilt: {Count} recipes indexed", recipes.Count);
    }
}
