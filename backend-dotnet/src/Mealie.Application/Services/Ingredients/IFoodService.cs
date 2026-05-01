using Mealie.Application.Dtos.Ingredients;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Ingredients;

public interface IFoodService
{
    Task<PaginatedResponse<FoodResponse>> GetFoodsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default);

    Task<FoodResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<FoodResponse> CreateAsync(Guid groupId, CreateFoodRequest request, CancellationToken ct = default);
    Task<FoodResponse?> UpdateAsync(Guid groupId, Guid id, UpdateFoodRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<bool> MergeAsync(Guid groupId, Guid fromFoodId, Guid toFoodId, CancellationToken ct = default);
}
