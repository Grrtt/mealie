using Mealie.Application.Dtos.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetCategoriesQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<CategoryResponse>>
{
    public async Task<PaginatedResponse<CategoryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(c => c.Name.Contains(Search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(c => new CategoryResponse
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt
            })
            .ToListAsync(ct);
        return new PaginatedResponse<CategoryResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}
