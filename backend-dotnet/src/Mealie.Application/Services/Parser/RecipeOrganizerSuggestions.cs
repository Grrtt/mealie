namespace Mealie.Application.Services.Parser;

/// <summary>
/// AI-suggested categories and tags to supplement what was scraped from the website.
/// </summary>
public record RecipeOrganizerSuggestions(
    IList<string> Categories,
    IList<string> Tags);
