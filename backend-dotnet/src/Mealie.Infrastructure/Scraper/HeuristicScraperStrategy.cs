using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Mealie.Infrastructure.Scraper;

/// <summary>
/// Last-resort heuristic scraper using CSS selector patterns to find recipe-like content.
/// </summary>
public class HeuristicScraperStrategy
{
    public async Task<ScrapedRecipeDto?> ScrapeAsync(string html)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);

        var title = document.QuerySelector("h1")?.TextContent?.Trim()
            ?? document.Title;

        if (string.IsNullOrWhiteSpace(title)) return null;

        // Try to find ingredients via common class names
        var ingredients = FindListItems(document, new[]
        {
            ".recipe-ingredients li",
            ".ingredients li",
            "[class*='ingredient'] li",
            ".wprm-recipe-ingredient",
            ".tasty-recipes-ingredients li",
        });

        // Try to find instructions via common class names
        var instructions = FindListItems(document, new[]
        {
            ".recipe-instructions li",
            ".instructions li",
            "[class*='instruction'] li",
            ".wprm-recipe-instruction-text",
            ".tasty-recipes-instructions li",
        });

        // Only return a result if we found some structured content
        if (ingredients.Count == 0 && instructions.Count == 0) return null;

        return new ScrapedRecipeDto
        {
            Name = title,
            Description = document.QuerySelector("[class*='description'], .recipe-description")?.TextContent?.Trim(),
            RecipeIngredient = ingredients,
            RecipeInstructions = instructions,
        };
    }

    private static IList<string> FindListItems(IDocument document, IEnumerable<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var elements = document.QuerySelectorAll(selector);
            var results = elements
                .Select(e => e.TextContent.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            if (results.Count > 0) return results;
        }
        return [];
    }
}
