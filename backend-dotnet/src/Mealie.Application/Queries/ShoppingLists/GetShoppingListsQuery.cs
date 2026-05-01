using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListsQuery(Guid HouseholdId, PaginationParams Pagination)
    : IQuery<PaginatedResponse<ShoppingListSummaryResponse>>
{
    public async Task<PaginatedResponse<ShoppingListSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.ShoppingLists.IgnoreQueryFilters().Where(s => s.HouseholdId == HouseholdId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(s => new ShoppingListSummaryResponse
            {
                Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId,
                CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt
            })
            .ToListAsync(ct);
        return new PaginatedResponse<ShoppingListSummaryResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}
