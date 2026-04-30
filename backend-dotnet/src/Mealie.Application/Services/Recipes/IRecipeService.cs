using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.IngredientParser;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeService
{
    Task<IList<RecipeSummaryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<PaginatedResponse<RecipeSummaryResponse>> GetPaginatedAsync(Guid householdId, PaginationParams pagination, RecipeFilter? filter = null, CancellationToken ct = default);
    Task<RecipeDetailResponse?> GetDetailBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<RecipeSummaryResponse?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<RecipeDetailResponse> CreateAsync(Guid groupId, Guid householdId, Guid userId, CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeDetailResponse?> UpdateAsync(Guid groupId, string slug, UpdateRecipeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<RecipeDetailResponse?> DuplicateAsync(Guid groupId, Guid householdId, Guid userId, string slug, CancellationToken ct = default);
    Task BulkDeleteAsync(IList<string> slugs, CancellationToken ct = default);
    Task BulkTagAsync(IList<string> slugs, IList<string> tagNames, Guid groupId, CancellationToken ct = default);
    Task BulkCategorizeAsync(IList<string> slugs, IList<string> categoryNames, Guid groupId, CancellationToken ct = default);
    Task<RecipeSuggestionsResponse> GetSuggestionsAsync(Guid householdId, Guid groupId, int limit, string? queryFilter, int maxMissingFoods, int maxMissingTools, bool includeFoodsOnHand, bool includeToolsOnHand, IList<Guid> foodIds, IList<Guid> toolIds, CancellationToken ct = default);
    Task<RecipeSummaryResponse?> CreateFromScrapedAsync(
        ScrapedRecipeDto scraped, Guid householdId, Guid groupId,
        IReadOnlyList<ParsedIngredientResult>? parsedIngredients = null,
        List<IngredientFood>? cachedFoods = null,
        List<IngredientUnit>? cachedUnits = null,
        CancellationToken ct = default);
}


public class RecipeFilter
{
    public string? Search { get; set; }
    public IList<string>? Tags { get; set; }
    public IList<string>? Categories { get; set; }
    public IList<string>? Tools { get; set; }
    public IList<string>? Foods { get; set; }
    public IList<string>? Households { get; set; }
    public bool? RequireAllCategories { get; set; }
    public bool? RequireAllTags { get; set; }
    public bool? RequireAllTools { get; set; }
    public bool? RequireAllFoods { get; set; }
    public string? OrderBy { get; set; }
    public string? OrderDirection { get; set; }
    public string? QueryFilter { get; set; }

    public bool IsEmpty =>
        string.IsNullOrEmpty(Search) &&
        (Tags is null || Tags.Count == 0) &&
        (Categories is null || Categories.Count == 0) &&
        (Foods is null || Foods.Count == 0) &&
        (Tools is null || Tools.Count == 0) &&
        (Households is null || Households.Count == 0);
}
