using Mealie.Application.Dtos.Organizers;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Organizers;

public interface IOrganizerService
{
    // Tags
    Task<PaginatedResponse<TagResponse>> GetTagsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default);
    Task<TagResponse?> GetTagBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<TagResponse> CreateTagAsync(Guid groupId, CreateOrganizerRequest request, CancellationToken ct = default);
    Task<TagResponse?> UpdateTagAsync(Guid groupId, Guid id, UpdateOrganizerRequest request, CancellationToken ct = default);
    Task<bool> DeleteTagAsync(Guid groupId, Guid id, CancellationToken ct = default);

    // Categories
    Task<PaginatedResponse<CategoryResponse>> GetCategoriesAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default);
    Task<CategoryResponse?> GetCategoryBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<CategoryResponse> CreateCategoryAsync(Guid groupId, CreateOrganizerRequest request, CancellationToken ct = default);
    Task<CategoryResponse?> UpdateCategoryAsync(Guid groupId, Guid id, UpdateOrganizerRequest request, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(Guid groupId, Guid id, CancellationToken ct = default);

    // Tools
    Task<PaginatedResponse<ToolResponse>> GetToolsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default);
    Task<ToolResponse?> GetToolBySlugAsync(Guid groupId, string slug, CancellationToken ct = default);
    Task<ToolResponse> CreateToolAsync(Guid groupId, CreateToolRequest request, CancellationToken ct = default);
    Task<ToolResponse?> UpdateToolAsync(Guid groupId, Guid id, UpdateToolRequest request, CancellationToken ct = default);
    Task<bool> DeleteToolAsync(Guid groupId, Guid id, CancellationToken ct = default);
}
