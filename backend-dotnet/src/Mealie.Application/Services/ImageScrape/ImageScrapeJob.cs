namespace Mealie.Application.Services.ImageScrape;

public record ImageScrapeJob(Guid RecipeId, string? OrgUrl, string? DirectImageUrl = null);
