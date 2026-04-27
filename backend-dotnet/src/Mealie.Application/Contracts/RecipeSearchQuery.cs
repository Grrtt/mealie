namespace Mealie.Application.Contracts;

public record RecipeSearchQuery(
    Guid HouseholdId,
    string? Text = null,
    IList<string>? Tags = null,
    IList<string>? Categories = null,
    int Skip = 0,
    int Take = 20);
