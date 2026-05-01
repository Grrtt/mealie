using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Mealie.Infrastructure.Scraper.Importers;

public class ChowdownMigrationParser : MigrationParserBase
{
    public override bool CanParse(Stream input)
    {
        try
        {
            input.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
            return zip.Entries.Any(e => e.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
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

        foreach (var entry in zip.Entries.Where(e => e.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            using var reader = new StreamReader(entry.Open());
            var content = reader.ReadToEnd();
            var recipe = ParseMarkdownRecipe(content);
            if (recipe is not null)
            {
                yield return recipe;
            }
        }
    }

    private static ScrapedRecipeDto? ParseMarkdownRecipe(string content)
    {
        // Chowdown format: YAML front matter between --- delimiters
        var match = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);
        if (!match.Success)
        {
            return null;
        }

        var frontMatter = match.Groups[1].Value;
        var body = match.Groups[2].Value;

        var recipe = new ScrapedRecipeDto();

        foreach (var line in frontMatter.Split('\n'))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
            {
                continue;
            }

            var key = line[..colonIdx].Trim().ToLower();
            var value = line[(colonIdx + 1)..].Trim().Trim('"').Trim('\'');
            switch (key)
            {
                case "title": recipe.Name = value; break;
                case "description": recipe.Description = value; break;
                case "image": recipe.Image = value; break;
            }
        }

        // Parse ingredients list from front matter
        var ingredientsMatch = Regex.Match(frontMatter, @"ingredients:(.*?)(?:^---|\z)",
            RegexOptions.Singleline | RegexOptions.Multiline);
        if (ingredientsMatch.Success)
        {
            recipe.RecipeIngredient = Regex
                .Matches(ingredientsMatch.Groups[1].Value, @"^\s*-\s*(.+)$", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value.Trim()).ToList();
        }

        // Body = instructions
        recipe.RecipeInstructions = body.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'))
            .Select(l => l.Trim()).ToList();

        return recipe;
    }
}
