using Mealie.Application.Dtos.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetToolsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<ToolResponse>>
{
    public async Task<PaginatedResponse<ToolResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(t => t.Name.Contains(Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(t => new ToolResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, OnHand = t.OnHand, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt })
            .ToListAsync(ct);
        return new PaginatedResponse<ToolResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}
