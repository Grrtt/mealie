using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Mealie.Application.Contracts.Search;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Directory = System.IO.Directory;

namespace Mealie.Application.Services.Search;

public sealed class LuceneFoodSearchIndex : IFoodSearchIndex, IIndexDiagnostics, IDisposable
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;
    private readonly StandardAnalyzer _analyzer;
    private readonly FSDirectory _directory;

    private readonly ILogger<LuceneFoodSearchIndex> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SearcherManager _searcherManager;
    private readonly IndexWriter _writer;

    public LuceneFoodSearchIndex(
        IOptions<AppSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<LuceneFoodSearchIndex> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        var indexPath = Path.Combine(settings.Value.DataDir, "search", "foods");
        Directory.CreateDirectory(indexPath);

        _directory = FSDirectory.Open(new DirectoryInfo(indexPath));
        _analyzer = new StandardAnalyzer(Version);
        var config = new IndexWriterConfig(Version, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };
        _writer = new IndexWriter(_directory, config);
        _searcherManager = new SearcherManager(_writer, true, null);
        _logger.LogInformation("LuceneFoodSearchIndex opened at {Path}", indexPath);
    }

    public void Dispose()
    {
        _searcherManager.Dispose();
        _writer.Dispose();
        _analyzer.Dispose();
        _directory.Dispose();
    }

    public async Task IndexAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var food = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.Id == id)
            .Include(f => f.Aliases)
            .FirstOrDefaultAsync(ct);

        if (food is null)
        {
            _writer.DeleteDocuments(new Term("id", id.ToString()));
            _writer.Commit();
            _searcherManager.MaybeRefreshBlocking();
            return;
        }

        _writer.UpdateDocument(new Term("id", food.Id.ToString()), BuildFoodDocument(food));
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        _logger.LogDebug("Indexed food {Name} ({Id})", food.Name, food.Id);
    }

    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _writer.DeleteDocuments(new Term("id", id.ToString()));
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        return Task.CompletedTask;
    }

    public Task<FoodSearchResult?> SearchAsync(FoodSearchQuery query, CancellationToken ct = default)
    {
        return Task.Run(() => DoSearch(query), ct);
    }

    public async Task RebuildAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding food search index from database");
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var foods = await db.Foods.IgnoreQueryFilters()
                .Include(f => f.Aliases)
                .ToListAsync(ct);

            _writer.DeleteAll();
            _writer.Commit();

            foreach (var food in foods)
            {
                _writer.AddDocument(BuildFoodDocument(food));
            }

            _writer.Commit();
            _searcherManager.MaybeRefreshBlocking();
            _logger.LogInformation("Food index rebuild complete — {Count} foods indexed", foods.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild food search index");
        }
    }

    // ── IIndexDiagnostics ────────────────────────────────────────────────────

    public string Name => "foods";

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
        _searcherManager.MaybeRefreshBlocking();
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
                    ["name", "name_lower", "alias_lower"],
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
                foreach (var fieldName in new[] { "id", "groupId", "name", "name_lower" })
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

    private FoodSearchResult? DoSearch(FoodSearchQuery query)
    {
        _searcherManager.MaybeRefreshBlocking();
        var searcher = _searcherManager.Acquire();
        try
        {
            var normalized = query.Text.Trim().ToLowerInvariant();
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var maxWords = Math.Min(words.Length, 5);
            for (var wordCount = maxWords; wordCount >= 1; wordCount--)
            {
                var candidate = string.Join(" ", words.Take(wordCount));

                var boolQuery = new BooleanQuery();
                boolQuery.Add(new TermQuery(new Term("groupId", query.GroupId.ToString())), Occur.MUST);

                var nameQuery = new BooleanQuery();
                nameQuery.Add(new TermQuery(new Term("name_lower", candidate)), Occur.SHOULD);
                nameQuery.Add(new TermQuery(new Term("plural_name_lower", candidate)), Occur.SHOULD);
                nameQuery.Add(new TermQuery(new Term("alias_lower", candidate)), Occur.SHOULD);
                boolQuery.Add(nameQuery, Occur.MUST);

                var hits = searcher.Search(boolQuery, 1);
                if (hits.TotalHits > 0)
                {
                    var doc = searcher.Doc(hits.ScoreDocs[0].Doc);
                    var foodId = Guid.Parse(doc.Get("id"));
                    var foodName = doc.Get("name");
                    var remainder = normalized.Length > candidate.Length
                        ? normalized[candidate.Length..].Trim().Trim(',').Trim()
                        : string.Empty;
                    return new FoodSearchResult(foodId, foodName, remainder);
                }
            }

            return null;
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    private static Document BuildFoodDocument(IngredientFood food)
    {
        var doc = new Document();
        doc.Add(new StringField("id", food.Id.ToString(), Field.Store.YES));
        doc.Add(new StringField("groupId", food.GroupId.ToString(), Field.Store.YES));
        doc.Add(new StringField("name", food.Name, Field.Store.YES));
        doc.Add(new StringField("name_lower", food.Name.ToLowerInvariant(), Field.Store.YES));
        if (!string.IsNullOrEmpty(food.PluralName))
        {
            doc.Add(new StringField("plural_name_lower", food.PluralName.ToLowerInvariant(), Field.Store.NO));
        }

        foreach (var alias in food.Aliases)
        {
            if (!string.IsNullOrEmpty(alias.Name))
            {
                doc.Add(new StringField("alias_lower", alias.Name.ToLowerInvariant(), Field.Store.NO));
            }
        }

        return doc;
    }
}
