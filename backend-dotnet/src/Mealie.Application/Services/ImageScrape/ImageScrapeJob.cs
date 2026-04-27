namespace Mealie.Application.Services.ImageScrape;

/// <param name="RecipeId">The recipe to attach the image to.</param>
/// <param name="OrgUrl">Page URL to scrape for an og:image (used when DirectImageUrl is null).</param>
/// <param name="DirectImageUrl">If set, download this URL directly instead of scraping OrgUrl.</param>
public record ImageScrapeJob(Guid RecipeId, string? OrgUrl, string? DirectImageUrl = null);
