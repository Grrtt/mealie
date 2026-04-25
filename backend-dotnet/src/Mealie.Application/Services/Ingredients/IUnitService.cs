using Mealie.Application.Dtos.Ingredients;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.Ingredients;

public interface IUnitService
{
    Task<PaginatedResponse<UnitResponse>> GetUnitsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default);
    Task<UnitResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default);
    Task<UnitResponse> CreateAsync(Guid groupId, CreateUnitRequest request, CancellationToken ct = default);
    Task<UnitResponse?> UpdateAsync(Guid groupId, Guid id, UpdateUnitRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default);
}
