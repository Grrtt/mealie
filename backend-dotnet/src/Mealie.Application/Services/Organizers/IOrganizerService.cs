using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Organizers;

public interface IOrganizerService
{
    // Tags
    Task<PaginatedResponse<TagResponse>> GetTagsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default);

    Task<TagResponse?> GetTagBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<TagResponse> CreateTagAsync(Guid groupId, CreateOrganizerRequest request, CancellationToken ct = default);

    Task<TagResponse?> UpdateTagAsync(Guid groupId, Guid id, UpdateOrganizerRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteTagAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<IList<TagResponse>> GetEmptyTagsAsync(Guid groupId, CancellationToken ct = default);
    Task<IList<RecipeSummaryResponse>> GetRecipesByTagAsync(Guid groupId, Guid tagId, CancellationToken ct = default);

    // Categories
    Task<PaginatedResponse<CategoryResponse>> GetCategoriesAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default);

    Task<CategoryResponse?> GetCategoryBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);

    Task<CategoryResponse> CreateCategoryAsync(Guid groupId, CreateOrganizerRequest request,
        CancellationToken ct = default);

    Task<CategoryResponse?> UpdateCategoryAsync(Guid groupId, Guid id, UpdateOrganizerRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteCategoryAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<IList<CategoryResponse>> GetEmptyCategoriesAsync(Guid groupId, CancellationToken ct = default);

    Task<IList<RecipeSummaryResponse>> GetRecipesByCategoryAsync(Guid groupId, Guid categoryId,
        CancellationToken ct = default);

    // Tools
    Task<PaginatedResponse<ToolResponse>> GetToolsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default);

    Task<ToolResponse?> GetToolBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<ToolResponse> CreateToolAsync(Guid groupId, CreateToolRequest request, CancellationToken ct = default);

    Task<ToolResponse?> UpdateToolAsync(Guid groupId, Guid id, UpdateToolRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteToolAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<IList<RecipeSummaryResponse>> GetRecipesByToolAsync(Guid groupId, Guid toolId, CancellationToken ct = default);
}
