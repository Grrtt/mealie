namespace Mealie.Application.Services.Parser;

/// <summary>
/// Implemented by AI parser strategies that can also suggest categories and tags for a recipe.
/// Non-AI strategies (NLP, brute) do not implement this interface.
/// </summary>
public interface IOrganizerAiStrategy
{
    Task<RecipeOrganizerSuggestions?> SuggestOrganizersAsync(
        RecipeOrganizerContext context, CancellationToken ct = default);
}
