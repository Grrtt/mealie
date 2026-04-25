namespace Mealie.Infrastructure.Scraper;

public class RecipeScraperService(HttpClient httpClient) : IRecipeScraperService
{
    private readonly JsonLdScraperStrategy _jsonLd = new();
    private readonly MicrodataScraperStrategy _microdata = new();
    private readonly HeuristicScraperStrategy _heuristic = new();

    public async Task<ScrapedRecipeDto> ScrapeAsync(string url, CancellationToken ct = default)
    {
        string html;
        try
        {
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return new ScrapedRecipeDto { ScrapingNotSupported = true };
        }

        // Strategy 1: JSON-LD (most reliable)
        var result = _jsonLd.Scrape(html);
        if (result is not null) return result;

        // Strategy 2: Microdata
        result = _microdata.Scrape(html);
        if (result is not null) return result;

        // Strategy 3: Heuristic CSS selectors
        result = await _heuristic.ScrapeAsync(html);
        if (result is not null) return result;

        return new ScrapedRecipeDto { ScrapingNotSupported = true };
    }
}
