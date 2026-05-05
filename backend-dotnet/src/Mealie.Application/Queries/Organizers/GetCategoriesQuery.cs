using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Queries.Organizers;

public record GetCategoriesQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<CategoryResponse>>
{
    public async Task<PaginatedResponse<CategoryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetCategoriesAsync(services.Db, GroupId, Pagination, Search, ct);
    }
}
