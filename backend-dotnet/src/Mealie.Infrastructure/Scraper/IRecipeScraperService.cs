namespace Mealie.Infrastructure.Scraper;

public interface IRecipeScraperService
{
    Task<ScrapedRecipeDto> ScrapeAsync(string url, CancellationToken ct = default);
}
