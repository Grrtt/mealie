using HtmlAgilityPack;
using Mealie.Application.Services.Images;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.ImageScrape;

/// <summary>
/// Long-running background service that downloads og:image (or first img) for recipes
/// that were imported without images. Rate-limited to one scrape per second.
/// </summary>
public class ImageScrapeBackgroundService(
    ImageScrapeQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<AppSettings> appSettings,
    ILogger<ImageScrapeBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Image scrape background service started");

        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing image scrape job for recipe {RecipeId}", job.RecipeId);
            }
        }
    }

    private async Task ProcessJobAsync(ImageScrapeJob job, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Processing image scrape job for recipe {RecipeId} (DirectImageUrl={DirectImageUrl}, OrgUrl={OrgUrl})",
                job.RecipeId, job.DirectImageUrl, job.OrgUrl);
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

            // If caller already knows the image URL, download it directly.
            // Otherwise, scrape the recipe page to find an og:image.
            string? imageUrl = job.DirectImageUrl;
            if (imageUrl is null)
            {
                if (job.OrgUrl is null)
                {
                    logger.LogWarning("ImageScrapeJob for recipe {RecipeId} has neither DirectImageUrl nor OrgUrl", job.RecipeId);
                    return;
                }

                string html;
                try
                {
                    html = await http.GetStringAsync(job.OrgUrl, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch page for recipe {RecipeId} from {OrgUrl}", job.RecipeId, job.OrgUrl);
                    return;
                }

                imageUrl = ExtractImageUrl(html);
            }

            if (imageUrl is null)
            {
                logger.LogInformation("No image found for recipe {RecipeId}", job.RecipeId);
                return;
            }

            byte[] imageBytes;
            try
            {
                imageBytes = await http.GetByteArrayAsync(imageUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to download image {ImageUrl} for recipe {RecipeId}", imageUrl, job.RecipeId);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var recipe = await db.Recipes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == job.RecipeId, ct);

            if (recipe is null)
            {
                logger.LogWarning("Recipe {RecipeId} not found during image scrape", job.RecipeId);
                return;
            }

            if (recipe.Image == "original.webp")
            {
                logger.LogInformation("Recipe {RecipeId} already has a local image, skipping", job.RecipeId);
                return;
            }

            var dir = Path.Combine(appSettings.Value.DataDir, "recipes", job.RecipeId.ToString(), "images");
            RecipeImageProcessor.SaveVariants(dir, imageBytes);

            recipe.Image = "original.webp";
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Saved scraped image for recipe {RecipeId} from {OrgUrl}", job.RecipeId, job.OrgUrl);
        }
        finally
        {
            try { await Task.Delay(1000, ct); }
            catch (OperationCanceledException) { /* shutting down */ }
        }
    }

    private static string? ExtractImageUrl(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Try og:image meta tag (property or name attribute)
        var ogImage = doc.DocumentNode
            .SelectNodes("//meta[@property='og:image' or @name='og:image']")
            ?.FirstOrDefault();
        if (ogImage is not null)
        {
            var content = ogImage.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(content)) return content;
        }

        // Fall back to first <img src="..."> that starts with http
        var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgNodes is not null)
        {
            foreach (var img in imgNodes)
            {
                var src = img.GetAttributeValue("src", "");
                if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return src;
            }
        }

        return null;
    }
}
