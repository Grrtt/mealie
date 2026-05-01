using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListItemsQuery(Guid HouseholdId, PaginationParams Pagination, bool? Checked = null)
    : IQuery<PaginatedResponse<ShoppingListItemResponse>>
{
    public async Task<PaginatedResponse<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.ShoppingListItems
            .Include(i => i.Unit).Include(i => i.Food)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId)
            .AsQueryable();
        if (Checked.HasValue) query = query.Where(i => i.Checked == Checked.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.UpdateAt).ThenBy(i => i.Position)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(i => ShoppingListItemMappings.MapItemToResponse(i))
            .ToListAsync(ct);
        return new PaginatedResponse<ShoppingListItemResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}

file static class ShoppingListItemMappings
{
    public static ShoppingListItemResponse MapItemToResponse(ShoppingListItem i) =>
        new()
        {
            Id = i.Id, Note = i.Note, IsFood = i.IsFood, Checked = i.Checked,
            DisableAmount = i.DisableAmount, Quantity = i.Quantity,
            ShoppingListId = i.ShoppingListId, UnitId = i.UnitId, FoodId = i.FoodId, LabelId = i.LabelId,
            Position = i.Position, UnitName = i.Unit?.Name, FoodName = i.Food?.Name,
            CreatedAt = i.CreatedAt, UpdateAt = i.UpdateAt
        };
}
