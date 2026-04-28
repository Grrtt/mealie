namespace Mealie.Application.Contracts.Search;

public record RecipeSearchResult(IList<string> Slugs, int Total);
