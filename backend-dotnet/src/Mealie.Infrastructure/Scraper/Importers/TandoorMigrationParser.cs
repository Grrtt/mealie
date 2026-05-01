using System.IO.Compression;
using System.Text.Json;

namespace Mealie.Infrastructure.Scraper.Importers;

/// <summary>
///     Parses Tandoor recipe manager exports (ZIP containing JSON files).
/// </summary>
public class TandoorMigrationParser : MigrationParserBase
{
    public override bool CanParse(Stream input)
    {
        try
        {
            input.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
            // Tandoor exports contain a recipes.json or individual recipe JSON files with "name" and "steps" fields
            return zip.Entries.Any(e => e.Name.EndsWith(".json") && IsTandoorRecipe(zip, e));
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

    private static bool IsTandoorRecipe(ZipArchive zip, ZipArchiveEntry entry)
    {
        try
        {
            using var stream = entry.Open();
            var doc = JsonDocument.Parse(stream).RootElement;
            // Tandoor JSON has "steps" (array of step objects with "ingredients") and "name"
            return doc.TryGetProperty("steps", out _) && doc.TryGetProperty("name", out _);
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
                if (!doc.TryGetProperty("steps", out _) || !doc.TryGetProperty("name", out _))
                {
                    continue;
                }

                results.Add(ParseTandoorRecipe(doc));
            }
            catch
            {
                /* skip malformed */
            }
        }

        return results;
    }

    private static ScrapedRecipeDto ParseTandoorRecipe(JsonElement doc)
    {
        var recipe = new ScrapedRecipeDto
        {
            Name = doc.TryGetProperty("name", out var n) ? n.GetString() : null,
            Description = doc.TryGetProperty("description", out var d) ? d.GetString() : null,
            RecipeYield = doc.TryGetProperty("servings", out var srv) ? srv.GetRawText() : null
        };

        if (doc.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
        {
            var instructions = new List<string>();
            var ingredients = new List<string>();

            foreach (var step in steps.EnumerateArray())
            {
                if (step.TryGetProperty("instruction", out var inst) && !string.IsNullOrWhiteSpace(inst.GetString()))
                {
                    instructions.Add(inst.GetString()!.Trim());
                }

                if (step.TryGetProperty("ingredients", out var ings) && ings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ing in ings.EnumerateArray())
                    {
                        var parts = new List<string>();
                        if (ing.TryGetProperty("amount", out var amt))
                        {
                            parts.Add(amt.GetRawText());
                        }

                        if (ing.TryGetProperty("unit", out var unit) && unit.TryGetProperty("name", out var unitName))
                        {
                            parts.Add(unitName.GetString() ?? "");
                        }

                        if (ing.TryGetProperty("food", out var food) && food.TryGetProperty("name", out var foodName))
                        {
                            parts.Add(foodName.GetString() ?? "");
                        }

                        if (ing.TryGetProperty("note", out var note) && !string.IsNullOrWhiteSpace(note.GetString()))
                        {
                            parts.Add($"({note.GetString()})");
                        }

                        ingredients.Add(string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p))));
                    }
                }
            }

            recipe.RecipeInstructions = instructions;
            recipe.RecipeIngredient = ingredients;
        }

        return recipe;
    }
}
