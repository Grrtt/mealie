namespace Mealie.Application.Services.Parser;

/// <summary>
/// Recipe context passed to an AI strategy when requesting organizer suggestions (categories and tags).
/// </summary>
public record RecipeOrganizerContext(
    string RecipeName,
    string? Description,
    IList<string> Ingredients,
    IList<string> WebsiteCategories,
    IList<string> WebsiteTags);
