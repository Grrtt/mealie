using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Mealie.Application.Dtos.Recipes;
using Mealie.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Search;

public record RecipeSearchResult(IList<string> Slugs, int Total);

/// <summary>
/// Singleton service that manages a Lucene FSDirectory index for fast recipe search.
/// Stored at {DataDir}/search/recipes/.
/// </summary>
public sealed class RecipeSearchIndex : IDisposable
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;

    private readonly ILogger<RecipeSearchIndex> _logger;
    private readonly Analyzer _analyzer;
    private readonly FSDirectory _directory;
    private readonly IndexWriter _writer;
    private readonly SearcherManager _searcherManager;

    public RecipeSearchIndex(IOptions<AppSettings> settings, ILogger<RecipeSearchIndex> logger)
    {
        _logger = logger;
        _analyzer = new StandardAnalyzer(Version);

        var indexPath = Path.Combine(settings.Value.DataDir, "search", "recipes");
        System.IO.Directory.CreateDirectory(indexPath);

        _directory = FSDirectory.Open(new DirectoryInfo(indexPath));
        var config = new IndexWriterConfig(Version, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };
        _writer = new IndexWriter(_directory, config);
        _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, searcherFactory: null);

        _logger.LogInformation("RecipeSearchIndex opened at {Path}", indexPath);
    }

    public void IndexRecipe(RecipeSummaryResponse recipe)
    {
        var doc = BuildDocument(recipe);
        _writer.UpdateDocument(new Term("id", recipe.Id.ToString()), doc);
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        _logger.LogDebug("Indexed recipe {Slug} ({Id})", recipe.Slug, recipe.Id);
    }

    public void RemoveRecipe(Guid recipeId)
    {
        _writer.DeleteDocuments(new Term("id", recipeId.ToString()));
        _writer.Commit();
        _searcherManager.MaybeRefreshBlocking();
        _logger.LogDebug("Removed recipe {Id} from index", recipeId);
    }

    /// <summary>
    /// Clears all documents from the index. Used before a full rebuild.
    /// </summary>
    public void ClearAllDocuments()
    {
        _writer.DeleteAll();
        _writer.Commit();
        _logger.LogInformation("Cleared all documents from search index");
    }

    public RecipeSearchResult Search(
        Guid householdId,
        string? query,
        IList<string>? tags,
        IList<string>? categories,
        int skip,
        int take)
    {
        _searcherManager.MaybeRefreshBlocking();
        var searcher = _searcherManager.Acquire();
        try
        {
            var mainQuery = new BooleanQuery();

            // Always filter by household
            mainQuery.Add(
                new TermQuery(new Term("householdId", householdId.ToString())),
                Occur.MUST);

            // Full-text search on name + description
            if (!string.IsNullOrWhiteSpace(query))
            {
                try
                {
                    var parser = new MultiFieldQueryParser(
                        Version,
                        ["name", "description"],
                        _analyzer);
                    parser.DefaultOperator = Operator.OR;
                    var textQuery = parser.Parse(QueryParser.Escape(query));
                    mainQuery.Add(textQuery, Occur.MUST);
                }
                catch (ParseException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse search query '{Query}', skipping text filter", query);
                }
            }

            // Tag filtering — require at least one provided tag to match
            if (tags is { Count: > 0 })
            {
                var tagQuery = BuildTermsQuery("tags", tags);
                if (tagQuery != null)
                    mainQuery.Add(tagQuery, Occur.MUST);
            }

            // Category filtering — require at least one provided category to match
            if (categories is { Count: > 0 })
            {
                var catQuery = BuildTermsQuery("categories", categories);
                if (catQuery != null)
                    mainQuery.Add(catQuery, Occur.MUST);
            }

            var sort = new Sort(new SortField("name_sort", SortFieldType.STRING));
            int needed = skip + take;
            if (needed <= 0) needed = take > 0 ? take : 10;

            var hits = searcher.Search(mainQuery, filter: null, n: needed, sort: sort);
            var total = hits.TotalHits;
            var slugs = hits.ScoreDocs
                .Skip(skip)
                .Take(take)
                .Select(sd =>
                {
                    var doc = searcher.Doc(sd.Doc);
                    return doc.Get("slug");
                })
                .Where(s => s is not null)
                .ToList();

            return new RecipeSearchResult(slugs!, total);
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    private BooleanQuery? BuildTermsQuery(string field, IList<string> terms)
    {
        var query = new BooleanQuery();
        foreach (var term in terms)
        {
            var tokens = AnalyzeString(field, term);
            foreach (var token in tokens)
                query.Add(new TermQuery(new Term(field, token)), Occur.SHOULD);
        }
        return query.Clauses.Count > 0 ? query : null;
    }

    private IList<string> AnalyzeString(string fieldName, string text)
    {
        var tokens = new List<string>();
        using var tokenStream = _analyzer.GetTokenStream(fieldName, text);
        tokenStream.Reset();
        var charTermAttr = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
        while (tokenStream.IncrementToken())
            tokens.Add(charTermAttr.ToString());
        tokenStream.End();
        return tokens;
    }

    private static Document BuildDocument(RecipeSummaryResponse recipe)
    {
        var doc = new Document();

        doc.Add(new StringField("id", recipe.Id.ToString(), Field.Store.YES));
        doc.Add(new StringField("householdId", recipe.HouseholdId.ToString(), Field.Store.YES));
        doc.Add(new TextField("name", recipe.Name, Field.Store.YES));
        doc.Add(new StringField("name_sort", recipe.Name.ToLowerInvariant(), Field.Store.NO));
        doc.Add(new StringField("slug", recipe.Slug, Field.Store.YES));

        if (!string.IsNullOrEmpty(recipe.Description))
            doc.Add(new TextField("description", recipe.Description, Field.Store.NO));

        if (recipe.Tags.Count > 0)
        {
            var tagText = string.Join(" ", recipe.Tags.Select(t => t.Name));
            doc.Add(new TextField("tags", tagText, Field.Store.NO));
        }

        if (recipe.Categories.Count > 0)
        {
            var catText = string.Join(" ", recipe.Categories.Select(c => c.Name));
            doc.Add(new TextField("categories", catText, Field.Store.NO));
        }

        return doc;
    }

    public void Dispose()
    {
        _searcherManager.Dispose();
        _writer.Dispose();
        _directory.Dispose();
        _analyzer.Dispose();
    }
}
