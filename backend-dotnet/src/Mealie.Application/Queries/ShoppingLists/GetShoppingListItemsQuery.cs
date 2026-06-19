using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListItemsQuery(Guid HouseholdId, PaginationParams Pagination, bool? Checked = null)
    : IQuery<PaginatedResponse<ShoppingListItemResponse>>
{
    public async Task<PaginatedResponse<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var query = ShoppingListMappingHelper.WithItemDetails(db.ShoppingListItems)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId)
            .AsQueryable();
        if (Checked.HasValue)
        {
            query = query.Where(i => i.Checked == Checked.Value);
        }

        var total = await query.CountAsync(ct);
        var paged = query.OrderByDescending(i => i.UpdateAt).ThenBy(i => i.Position).Skip(Pagination.Skip);
        if (Pagination.PerPage > 0) paged = paged.Take(Pagination.PerPage);
        var items = await paged.ToListAsync(ct);
        return new PaginatedResponse<ShoppingListItemResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = Pagination.PerPage > 0 ? (int)Math.Ceiling((double)total / Pagination.PerPage) : 1,
            Items = items.Select(ShoppingListMappingHelper.MapItemToResponse).ToList()
        };
    }
}
