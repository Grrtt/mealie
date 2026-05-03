namespace Mealie.Application.Services.Parser;

/// <summary>
/// High-level service for AI-powered recipe organizer suggestions.
/// Returns null when the configured parser is not an AI strategy.
/// </summary>
public interface IRecipeOrganizerService
{
    Task<RecipeOrganizerSuggestions?> SuggestAsync(
        string? parserKey,
        RecipeOrganizerContext context,
        CancellationToken ct = default);
}
