using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Queries.Organizers;

public record GetToolsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<ToolResponse>>
{
    public async Task<PaginatedResponse<ToolResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetToolsAsync(services.Db, GroupId, Pagination, Search, ct);
    }
}
