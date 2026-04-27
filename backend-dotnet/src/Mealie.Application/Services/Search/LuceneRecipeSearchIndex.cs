using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Mealie.Application.Contracts;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Search;

/// <summary>
/// Singleton Lucene.NET search index for recipes. Provides near-real-time search.
/// </summary>
public sealed class LuceneRecipeSearchIndex : IRecipeSearchIndex, IDisposable
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;

    private readonly StandardAnalyzer _analyzer;
    private readonly IndexWriter _writer;
    private readonly SearcherManager _searcherManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LuceneRecipeSearchIndex> _logger;

    public LuceneRecipeSearchIndex(
        IOptions<AppSettings> appSettings,
        IServiceScopeFactory scopeFactory,
        ILogger<LuceneRecipeSearchIndex> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var indexPath = Path.Combine(appSettings.Value.DataDir, "search", "recipes");
        System.IO.Directory.CreateDirectory(indexPath);

        var directory = FSDirectory.Open(new DirectoryInfo(indexPath));
        _analyzer = new StandardAnalyzer(Version);
        var config = new IndexWriterConfig(Version, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
        };
        _writer = new IndexWriter(directory, config);
        _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, searcherFactory: null);
    }

    public async Task IndexRecipeAsync(Guid recipeId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Id == recipeId)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            _logger.LogWarning("Recipe {RecipeId} not found for indexing", recipeId);
            return;
        }

        var doc = new Document();
        doc.Add(new StringField("id", recipe.Id.ToString(), Field.Store.YES));
        doc.Add(new StringField("householdId", recipe.HouseholdId.ToString(), Field.Store.YES));
        doc.Add(new StringField("slug", recipe.Slug, Field.Store.YES));
        doc.Add(new TextField("name", recipe.Name, Field.Store.YES));
        doc.Add(new StringField("name_sort", recipe.Name.ToLowerInvariant(), Field.Store.NO));

        if (!string.IsNullOrWhiteSpace(recipe.Description))
            doc.Add(new TextField("description", recipe.Description, Field.Store.NO));

        var tagsText = string.Join(" ", recipe.Tags.Select(t => t.Name));
        doc.Add(new TextField("tags", tagsText, Field.Store.NO));

        var categoriesText = string.Join(" ", recipe.Categories.Select(c => c.Name));
        doc.Add(new TextField("categories", categoriesText, Field.Store.NO));

        // Also store slugs for exact filtering
        foreach (var tag in recipe.Tags)
            doc.Add(new StringField("tag_slug", tag.Slug, Field.Store.NO));
        foreach (var cat in recipe.Categories)
            doc.Add(new StringField("cat_slug", cat.Slug, Field.Store.NO));

        _writer.UpdateDocument(new Term("id", recipe.Id.ToString()), doc);
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();

        _logger.LogDebug("Indexed recipe {RecipeId} ({Slug})", recipeId, recipe.Slug);
    }

    public Task RemoveRecipeAsync(Guid recipeId, CancellationToken ct = default)
    {
        _writer.DeleteDocuments(new Term("id", recipeId.ToString()));
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        _logger.LogDebug("Removed recipe {RecipeId} from index", recipeId);
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
            if (maxDocs <= 0) maxDocs = 20;

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
            if (ct.IsCancellationRequested) break;

            var doc = new Document();
            doc.Add(new StringField("id", recipe.Id.ToString(), Field.Store.YES));
            doc.Add(new StringField("householdId", recipe.HouseholdId.ToString(), Field.Store.YES));
            doc.Add(new StringField("slug", recipe.Slug, Field.Store.YES));
            doc.Add(new TextField("name", recipe.Name, Field.Store.YES));
            doc.Add(new StringField("name_sort", recipe.Name.ToLowerInvariant(), Field.Store.NO));

            if (!string.IsNullOrWhiteSpace(recipe.Description))
                doc.Add(new TextField("description", recipe.Description, Field.Store.NO));

            var tagsText = string.Join(" ", recipe.Tags.Select(t => t.Name));
            doc.Add(new TextField("tags", tagsText, Field.Store.NO));

            var categoriesText = string.Join(" ", recipe.Categories.Select(c => c.Name));
            doc.Add(new TextField("categories", categoriesText, Field.Store.NO));

            foreach (var tag in recipe.Tags)
                doc.Add(new StringField("tag_slug", tag.Slug, Field.Store.NO));
            foreach (var cat in recipe.Categories)
                doc.Add(new StringField("cat_slug", cat.Slug, Field.Store.NO));

            _writer.AddDocument(doc);
        }

        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();

        _logger.LogInformation("Recipe search index rebuilt: {Count} recipes indexed", recipes.Count);
    }

    public void Dispose()
    {
        _searcherManager.Dispose();
        _writer.Dispose();
        _analyzer.Dispose();
    }
}
