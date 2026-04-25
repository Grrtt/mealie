using System.IO.Compression;
using System.Text.Json;

namespace Mealie.Infrastructure.Scraper.Importers;

public class MealieBackupImportParser : MigrationParserBase
{
    public override bool CanParse(Stream input)
    {
        try
        {
            input.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
            return zip.Entries.Any(e => e.Name == "mealie_export.json" || e.FullName.Contains("recipes/"));
        }
        catch { return false; }
        finally { input.Seek(0, SeekOrigin.Begin); }
    }

    public override IEnumerable<ScrapedRecipeDto> Parse(Stream input)
    {
        input.Seek(0, SeekOrigin.Begin);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);

        var recipeEntries = zip.Entries.Where(e => e.FullName.Contains("/recipes/") && e.Name.EndsWith(".json"));
        foreach (var entry in recipeEntries)
        {
            using var stream = entry.Open();
            var doc = JsonDocument.Parse(stream).RootElement;
            yield return new ScrapedRecipeDto
            {
                Name = doc.TryGetProperty("name", out var n) ? n.GetString() : null,
                Description = doc.TryGetProperty("description", out var d) ? d.GetString() : null,
                RecipeIngredient = [],
                RecipeInstructions = [],
            };
        }
    }
}
