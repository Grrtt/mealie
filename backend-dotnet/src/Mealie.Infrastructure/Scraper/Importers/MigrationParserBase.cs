namespace Mealie.Infrastructure.Scraper.Importers;

public interface IMigrationParser
{
    bool CanParse(Stream input);
    IEnumerable<ScrapedRecipeDto> Parse(Stream input);
}

public abstract class MigrationParserBase : IMigrationParser
{
    public abstract bool CanParse(Stream input);
    public abstract IEnumerable<ScrapedRecipeDto> Parse(Stream input);

    protected static string? CleanText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    protected static IList<string> SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] :
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}
