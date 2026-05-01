using HtmlAgilityPack;

namespace Mealie.Infrastructure.Scraper;

/// <summary>
///     Fallback scraper that parses schema.org/Recipe microdata (itemprop attributes).
/// </summary>
public class MicrodataScraperStrategy
{
    public ScrapedRecipeDto? Scrape(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find the root Recipe itemscope element
        var recipeRoot = doc.DocumentNode.SelectSingleNode(
            "//*[@itemtype='http://schema.org/Recipe' or @itemtype='https://schema.org/Recipe']");
        if (recipeRoot is null)
        {
            return null;
        }

        return new ScrapedRecipeDto
        {
            Name = GetItemprop(recipeRoot, "name"),
            Description = GetItemprop(recipeRoot, "description"),
            Image = GetItempropAttr(recipeRoot, "image", "src") ?? GetItemprop(recipeRoot, "image"),
            RecipeYield = GetItemprop(recipeRoot, "recipeYield"),
            TotalTime = GetItemprop(recipeRoot, "totalTime"),
            PrepTime = GetItemprop(recipeRoot, "prepTime"),
            CookTime = GetItemprop(recipeRoot, "cookTime"),
            RecipeIngredient = GetAllItemprop(recipeRoot, "recipeIngredient"),
            RecipeInstructions = GetAllItemprop(recipeRoot, "recipeInstructions"),
            Keywords = GetKeywords(recipeRoot)
        };
    }

    private static string? GetItemprop(HtmlNode root, string prop)
    {
        var node = root.SelectSingleNode($".//*[@itemprop='{prop}']");
        if (node is null)
        {
            return null;
        }

        var content = node.GetAttributeValue("content", string.Empty);
        if (!string.IsNullOrEmpty(content))
        {
            return content;
        }

        var datetime = node.GetAttributeValue("datetime", string.Empty);
        if (!string.IsNullOrEmpty(datetime))
        {
            return datetime;
        }

        return node.InnerText.Trim();
    }

    private static string? GetItempropAttr(HtmlNode root, string prop, string attr)
    {
        var node = root.SelectSingleNode($".//*[@itemprop='{prop}']");
        if (node is null)
        {
            return null;
        }

        var val = node.GetAttributeValue(attr, string.Empty);
        return string.IsNullOrEmpty(val) ? null : val;
    }

    private static IList<string> GetAllItemprop(HtmlNode root, string prop)
    {
        var nodes = root.SelectNodes($".//*[@itemprop='{prop}']");
        if (nodes is null)
        {
            return [];
        }

        return nodes
            .Select(n =>
            {
                var content = n.GetAttributeValue("content", string.Empty);
                return !string.IsNullOrEmpty(content) ? content : n.InnerText.Trim();
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static IList<string> GetKeywords(HtmlNode root)
    {
        var raw = GetItemprop(root, "keywords");
        if (raw is null)
        {
            return [];
        }

        return raw.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
    }
}
