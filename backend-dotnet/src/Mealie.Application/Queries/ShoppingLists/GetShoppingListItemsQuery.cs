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
        var items = await query.OrderByDescending(i => i.UpdateAt).ThenBy(i => i.Position)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .ToListAsync(ct);
        return new PaginatedResponse<ShoppingListItemResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage),
            Items = items.Select(ShoppingListMappingHelper.MapItemToResponse).ToList()
        };
    }
}
