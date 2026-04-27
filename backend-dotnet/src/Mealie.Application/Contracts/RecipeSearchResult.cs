namespace Mealie.Application.Contracts;

public record RecipeSearchResult(IList<string> Slugs, int Total);
