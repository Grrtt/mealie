using Mealie.Application.Dtos.Organizers;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Cookbooks;

public interface ICookbookService
{
    Task<PaginatedResponse<CookbookResponse>> GetCookbooksAsync(Guid householdId, PaginationParams pagination, CancellationToken ct = default);
    Task<CookbookResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default);
    Task<CookbookResponse> CreateAsync(Guid groupId, Guid householdId, CreateCookbookRequest request, CancellationToken ct = default);
    Task<CookbookResponse?> UpdateAsync(Guid householdId, Guid id, UpdateCookbookRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);
}
