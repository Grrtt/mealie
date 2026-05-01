using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Mealie.Infrastructure.Scraper.Importers;

public class PaprikaMigrationParser : MigrationParserBase
{
    public override bool CanParse(Stream input)
    {
        try
        {
            input.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
            return zip.Entries.Any(e => e.Name.EndsWith(".paprikarecipe", StringComparison.OrdinalIgnoreCase));
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

    public override IEnumerable<ScrapedRecipeDto> Parse(Stream input)
    {
        input.Seek(0, SeekOrigin.Begin);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);

        foreach (var entry in zip.Entries.Where(e =>
                     e.Name.EndsWith(".paprikarecipe", StringComparison.OrdinalIgnoreCase)))
        {
            using var entryStream = entry.Open();
            using var gzip = new GZipStream(entryStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            var json = reader.ReadToEnd();

            var recipe = ParsePaprikaJson(json);
            if (recipe is not null)
            {
                yield return recipe;
            }
        }
    }

    private static ScrapedRecipeDto? ParsePaprikaJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json).RootElement;
            return new ScrapedRecipeDto
            {
                Name = doc.TryGetProperty("name", out var n) ? n.GetString() : null,
                Description = doc.TryGetProperty("description", out var d) ? d.GetString() : null,
                PrepTime = doc.TryGetProperty("prep_time", out var pt) ? pt.GetString() : null,
                CookTime = doc.TryGetProperty("cook_time", out var ct) ? ct.GetString() : null,
                RecipeYield = doc.TryGetProperty("servings", out var s) ? s.GetString() : null,
                RecipeIngredient = doc.TryGetProperty("ingredients", out var ing)
                    ? ing.GetString()?.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())
                        .ToList() ?? []
                    : [],
                RecipeInstructions = doc.TryGetProperty("directions", out var dir)
                    ? dir.GetString()?.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())
                        .ToList() ?? []
                    : []
            };
        }
        catch
        {
            return null;
        }
    }
}
