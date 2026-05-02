using HtmlAgilityPack;
using Schema.NET;

namespace Mealie.Infrastructure.Scraper;

public class JsonLdScraperStrategy
{
    public ScrapedRecipeDto? Scrape(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is null)
            return null;

        foreach (var script in scripts)
        {
            try
            {
                var json = script.InnerText.Trim();
                var result = TryParseBlock(json);
                if (result is not null)
                    return result;
            }
            catch
            {
                /* skip malformed JSON-LD */
            }
        }

        return null;
    }

    private static ScrapedRecipeDto? TryParseBlock(string json)
    {
        // Direct Recipe object
        try
        {
            var recipe = SchemaSerializer.DeserializeObject<Recipe>(json);
            if (recipe is not null)
                return MapRecipe(recipe);
        }
        catch { }

        // @graph: re-parse raw JSON to iterate graph nodes
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("@graph", out var graph))
            {
                foreach (var item in graph.EnumerateArray())
                {
                    if (!IsRecipeType(item)) continue;
                    try
                    {
                        var recipe = SchemaSerializer.DeserializeObject<Recipe>(item.GetRawText());
                        if (recipe is not null)
                            return MapRecipe(recipe);
                    }
                    catch { }
                }
            }
        }
        catch { }

        return null;
    }

    private static bool IsRecipeType(System.Text.Json.JsonElement el)
    {
        if (!el.TryGetProperty("@type", out var t)) return false;
        if (t.ValueKind == System.Text.Json.JsonValueKind.String)
            return t.GetString()?.Equals("Recipe", StringComparison.OrdinalIgnoreCase) ?? false;
        if (t.ValueKind == System.Text.Json.JsonValueKind.Array)
            return t.EnumerateArray().Any(e => e.GetString()?.Equals("Recipe", StringComparison.OrdinalIgnoreCase) ?? false);
        return false;
    }

    private static ScrapedRecipeDto MapRecipe(Recipe r) => new()
    {
        Name = r.Name.FirstOrDefault(),
        Description = r.Description.FirstOrDefault()?.ToString(),
        Image = ExtractImageUrl(r),
        RecipeYield = r.RecipeYield.FirstOrDefault()?.ToString(),
        TotalTime = FormatDuration(r.TotalTime.FirstOrDefault()),
        PrepTime = FormatDuration(r.PrepTime.FirstOrDefault()),
        CookTime = FormatDuration(r.CookTime.FirstOrDefault()),
        RecipeIngredient = r.RecipeIngredient.Select(v => v.ToString() ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
        RecipeInstructions = ExtractInstructions(r),
        Keywords = ExtractKeywords(r),
        Categories = r.RecipeCategory.Select(v => v.ToString() ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
        Nutrition = ExtractNutrition(r)
    };

    private static string? ExtractImageUrl(Recipe r)
    {
        var img = r.Image.FirstOrDefault();
        return img switch
        {
            Uri uri => uri.ToString(),
            ImageObject io => io.Url.FirstOrDefault()?.ToString(),
            string s => s,
            _ => null
        };
    }

    private static IList<string> ExtractInstructions(Recipe r)
    {
        var results = new List<string>();
        foreach (var item in r.RecipeInstructions)
        {
            switch (item)
            {
                case string s when !string.IsNullOrWhiteSpace(s):
                    results.Add(s);
                    break;
                case HowToStep step:
                    var text = step.Text.FirstOrDefault()?.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) results.Add(text);
                    break;
                case HowToSection section:
                    foreach (var sItem in section.ItemListElement)
                    {
                        if (sItem is HowToStep sStep)
                        {
                            var sText = sStep.Text.FirstOrDefault()?.ToString();
                            if (!string.IsNullOrWhiteSpace(sText)) results.Add(sText);
                        }
                    }
                    break;
            }
        }
        return results;
    }

    private static IList<string> ExtractKeywords(Recipe r)
    {
        var results = new List<string>();
        foreach (var kw in r.Keywords)
        {
            var s = kw.ToString() ?? "";
            // Keywords are often comma-separated in a single string
            results.AddRange(s.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)));
        }
        return results;
    }

    private static NutritionDto? ExtractNutrition(Recipe r)
    {
        var n = r.Nutrition.FirstOrDefault();
        if (n is not NutritionInformation ni) return null;
        return new NutritionDto
        {
            Calories = ni.Calories.FirstOrDefault()?.ToString(),
            FatContent = ni.FatContent.FirstOrDefault()?.ToString(),
            ProteinContent = ni.ProteinContent.FirstOrDefault()?.ToString(),
            CarbohydrateContent = ni.CarbohydrateContent.FirstOrDefault()?.ToString()
        };
    }

    private static string? FormatDuration(object? value) => value switch
    {
        TimeSpan ts when ts > TimeSpan.Zero => System.Xml.XmlConvert.ToString(ts),
        string s when !string.IsNullOrWhiteSpace(s) => s,
        _ => null
    };
}

