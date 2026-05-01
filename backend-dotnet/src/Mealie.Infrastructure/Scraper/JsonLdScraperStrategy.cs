using System.Text.Json;
using HtmlAgilityPack;

namespace Mealie.Infrastructure.Scraper;

public class JsonLdScraperStrategy
{
    public ScrapedRecipeDto? Scrape(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is null)
        {
            return null;
        }

        foreach (var script in scripts)
        {
            try
            {
                var json = script.InnerText.Trim();
                var element = JsonDocument.Parse(json).RootElement;

                // Handle @graph array
                if (element.TryGetProperty("@graph", out var graph))
                {
                    foreach (var item in graph.EnumerateArray())
                    {
                        var recipe = TryParseRecipe(item);
                        if (recipe is not null)
                        {
                            return recipe;
                        }
                    }
                }

                var directRecipe = TryParseRecipe(element);
                if (directRecipe is not null)
                {
                    return directRecipe;
                }
            }
            catch
            {
                /* skip malformed JSON-LD */
            }
        }

        return null;
    }

    private static ScrapedRecipeDto? TryParseRecipe(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var typeEl))
        {
            return null;
        }

        var type = typeEl.ValueKind == JsonValueKind.Array
            ? typeEl.EnumerateArray().Select(e => e.GetString()).FirstOrDefault()
            : typeEl.GetString();
        if (!string.Equals(type, "Recipe", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ScrapedRecipeDto
        {
            Name = GetString(element, "name"),
            Description = GetString(element, "description"),
            Image = GetImageUrl(element),
            RecipeYield = GetString(element, "recipeYield"),
            TotalTime = GetString(element, "totalTime"),
            PrepTime = GetString(element, "prepTime"),
            CookTime = GetString(element, "cookTime"),
            RecipeIngredient = GetStringArray(element, "recipeIngredient"),
            RecipeInstructions = GetInstructions(element),
            Keywords = GetStringArray(element, "keywords")
        };
    }

    private static string? GetString(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var v) ? v.GetString() : null;
    }

    private static string? GetImageUrl(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var img))
        {
            return null;
        }

        return img.ValueKind switch
        {
            JsonValueKind.String => img.GetString(),
            JsonValueKind.Array => img.EnumerateArray()
                .Select(e => e.TryGetProperty("url", out var u) ? u.GetString() : e.GetString())
                .FirstOrDefault(),
            JsonValueKind.Object => img.TryGetProperty("url", out var u) ? u.GetString() : null,
            _ => null
        };
    }

    private static IList<string> GetStringArray(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var arr))
        {
            return [];
        }

        if (arr.ValueKind == JsonValueKind.String)
        {
            return arr.GetString()?.Split(',').Select(s => s.Trim()).ToList() ?? [];
        }

        if (arr.ValueKind == JsonValueKind.Array)
        {
            return arr.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return [];
    }

    private static IList<string> GetInstructions(JsonElement el)
    {
        if (!el.TryGetProperty("recipeInstructions", out var arr))
        {
            return [];
        }

        if (arr.ValueKind == JsonValueKind.String)
        {
            return [arr.GetString() ?? ""];
        }

        if (arr.ValueKind == JsonValueKind.Array)
        {
            return arr.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.Object
                    ? (e.TryGetProperty("text", out var t) ? t.GetString() : e.GetString()) ?? ""
                    : e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return [];
    }
}
