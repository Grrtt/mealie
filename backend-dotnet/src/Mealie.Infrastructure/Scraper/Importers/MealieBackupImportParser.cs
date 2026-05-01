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
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
            return zip.Entries.Any(e => e.Name == "mealie_export.json" || e.FullName.Contains("recipes/"));
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

        var recipeEntries = zip.Entries.Where(e => e.FullName.Contains("/recipes/") && e.Name.EndsWith(".json"));
        foreach (var entry in recipeEntries)
        {
            using var stream = entry.Open();
            var doc = JsonDocument.Parse(stream).RootElement;

            // The recipe folder is the parent directory of the .json file
            var folderPrefix = entry.FullName[..entry.FullName.LastIndexOf('/')];

            // Collect any image files sitting alongside the json (e.g. images/ subfolder)
            var imageFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var imgEntry in zip.Entries.Where(e =>
                         e.FullName.StartsWith(folderPrefix + "/images/", StringComparison.OrdinalIgnoreCase)
                         && e.Length > 0))
            {
                using var imgStream = imgEntry.Open();
                using var ms = new MemoryStream();
                imgStream.CopyTo(ms);
                imageFiles[imgEntry.Name] = ms.ToArray();
            }

            yield return new ScrapedRecipeDto
            {
                Name = doc.TryGetProperty("name", out var n) ? n.GetString() : null,
                Description = doc.TryGetProperty("description", out var d) ? d.GetString() : null,
                RecipeIngredient = [],
                RecipeInstructions = [],
                ImageFiles = imageFiles
            };
        }
    }
}
