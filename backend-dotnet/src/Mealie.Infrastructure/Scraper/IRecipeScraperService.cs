namespace Mealie.Infrastructure.Scraper;

public interface IRecipeScraperService
{
    Task<ScrapedRecipeDto> ScrapeAsync(string url, string? userAgent = null, CancellationToken ct = default);
    Task<ScrapedRecipeDto> ScrapeFromHtmlAsync(string html, string? sourceUrl = null, CancellationToken ct = default);
}
