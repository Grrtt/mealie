using System.IO.Compression;
using System.Text.Json;

namespace Mealie.Infrastructure.Scraper.Importers;

public class NextcloudCookbookMigrationParser : MigrationParserBase
{
    public override bool CanParse(Stream input)
    {
        try
        {
            input.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
            return zip.Entries.Any(e => e.Name.EndsWith(".json") && IsSchemaOrgRecipe(zip, e));
        }
        catch
        {
            return false;
        }
        finally
        {
            input.Seek(0, SeekOrigin.Begin);
        }
    }

    private static bool IsSchemaOrgRecipe(ZipArchive zip, ZipArchiveEntry entry)
    {
        try
        {
            using var stream = entry.Open();
            var doc = JsonDocument.Parse(stream).RootElement;
            return doc.TryGetProperty("@type", out var t) && t.GetString() == "Recipe";
        }
        catch
        {
            return false;
        }
    }

    public override IEnumerable<ScrapedRecipeDto> Parse(Stream input)
    {
        input.Seek(0, SeekOrigin.Begin);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);

        var results = new List<ScrapedRecipeDto>();
        foreach (var entry in zip.Entries.Where(e => e.Name.EndsWith(".json")))
        {
            try
            {
                using var stream = entry.Open();
                var doc = JsonDocument.Parse(stream).RootElement;
                if (!doc.TryGetProperty("@type", out var t) || t.GetString() != "Recipe")
                {
                    continue;
                }

                results.Add(new ScrapedRecipeDto
                {
                    Name = doc.TryGetProperty("name", out var n) ? n.GetString() : null,
                    Description = doc.TryGetProperty("description", out var d) ? d.GetString() : null,
                    PrepTime = doc.TryGetProperty("prepTime", out var pt) ? pt.GetString() : null,
                    CookTime = doc.TryGetProperty("cookTime", out var ct) ? ct.GetString() : null,
                    TotalTime = doc.TryGetProperty("totalTime", out var tt) ? tt.GetString() : null,
                    RecipeYield = doc.TryGetProperty("recipeYield", out var s)
                        ? s.ValueKind == JsonValueKind.Array
                            ? s.EnumerateArray().FirstOrDefault().GetString()
                            : s.GetString()
                        : null,
                    RecipeIngredient = GetStringArray(doc, "recipeIngredient"),
                    RecipeInstructions = GetInstructionSteps(doc),
                    Keywords = GetStringOrArray(doc, "keywords"),
                    Categories = GetStringOrArray(doc, "recipeCategory"),
                    OrgUrl = doc.TryGetProperty("url", out var u) ? u.GetString() : null
                });
            }
            catch
            {
                /* skip malformed */
            }
        }

        return results;
    }

    private static IList<string> GetStringArray(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var arr))
        {
            return [];
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

    /// <summary>
    ///     Schema.org fields like "keywords" and "recipeCategory" can be either an array of strings
    ///     OR a single comma-separated string. This handles both.
    /// </summary>
    private static IList<string> GetStringOrArray(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var val))
        {
            return [];
        }

        if (val.ValueKind == JsonValueKind.Array)
        {
            return val.EnumerateArray()
                .Select(e => e.GetString()?.Trim() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (val.ValueKind == JsonValueKind.String)
        {
            var str = val.GetString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return [];
            }

            return str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return [];
    }

    private static IList<string> GetInstructionSteps(JsonElement doc)
    {
        if (!doc.TryGetProperty("recipeInstructions", out var arr))
        {
            return [];
        }

        if (arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return arr.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Object
                ? (e.TryGetProperty("text", out var txt) ? txt.GetString() : null) ?? ""
                : e.GetString() ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
