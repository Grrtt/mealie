namespace Mealie.Infrastructure.Scraper;

public interface IRecipeScraperService
{
    Task<ScrapedRecipeDto> ScrapeAsync(string url, CancellationToken ct = default);
    Task<ScrapedRecipeDto> ScrapeFromHtmlAsync(string html, string? sourceUrl = null, CancellationToken ct = default);
}
