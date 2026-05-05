using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Queries.Organizers;

public record GetTagsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<TagResponse>>
{
    public async Task<PaginatedResponse<TagResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetTagsAsync(services.Db, GroupId, Pagination, Search, ct);
    }
}
