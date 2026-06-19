using Mealie.Infrastructure.Scraper;

namespace Mealie.Application.Services.Ocr;

/// <summary>
/// Service that uses an AI vision model to extract structured recipe data
/// from an image (cookbook page, handwritten note, screenshot, etc.).
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Sends the image to an OpenAI-compatible vision API and parses the
    /// response into a <see cref="ScrapedRecipeDto"/>.
    /// Returns <c>null</c> when the active AI configuration is missing,
    /// image services are disabled, the API call fails, or the response
    /// cannot be parsed.
    /// </summary>
    Task<ScrapedRecipeDto?> ExtractRecipeFromImageAsync(byte[] imageBytes, CancellationToken ct = default);
}
