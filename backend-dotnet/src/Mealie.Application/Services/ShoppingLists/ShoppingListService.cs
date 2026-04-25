using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.ShoppingLists;

public class ShoppingListService(ApplicationDbContext db) : IShoppingListService
{
    public async Task<PaginatedResponse<ShoppingListSummaryResponse>> GetShoppingListsAsync(Guid householdId, PaginationParams pagination, CancellationToken ct = default)
    {
        var query = db.ShoppingLists.IgnoreQueryFilters().Where(s => s.HouseholdId == householdId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(s => new ShoppingListSummaryResponse { Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId, CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt })
            .ToListAsync(ct);
        return new PaginatedResponse<ShoppingListSummaryResponse> { Page = pagination.Page, PerPage = pagination.PerPage, Total = total, TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items };
    }

    public async Task<ShoppingListResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId && s.Id == id)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;
        return MapToResponse(list);
    }

    public async Task<ShoppingListResponse> CreateAsync(Guid groupId, Guid householdId, CreateShoppingListRequest request, CancellationToken ct = default)
    {
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(), Name = request.Name,
            GroupId = groupId, HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        return MapToResponse(list);
    }

    public async Task<ShoppingListResponse?> UpdateAsync(Guid householdId, Guid id, UpdateShoppingListRequest request, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId && s.Id == id)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;
        if (request.Name is not null) list.Name = request.Name;
        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(list);
    }

    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == householdId && s.Id == id, ct);
        if (list is null) return false;
        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShoppingListItemResponse> AddItemAsync(Guid householdId, Guid listId, CreateShoppingListItemRequest request, CancellationToken ct = default)
    {
        _ = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == householdId && s.Id == listId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");

        var position = await db.ShoppingListItems.Where(i => i.ShoppingListId == listId).MaxAsync(i => (int?)i.Position, ct) ?? -1;
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(), Note = request.Note, IsFood = request.IsFood,
            DisableAmount = request.DisableAmount, Quantity = request.Quantity,
            UnitId = request.UnitId, FoodId = request.FoodId, LabelId = request.LabelId,
            Position = position + 1, ShoppingListId = listId, Checked = false,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
        return MapItemToResponse(item);
    }

    public async Task<ShoppingListItemResponse?> UpdateItemAsync(Guid householdId, Guid listId, Guid itemId, UpdateShoppingListItemRequest request, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems
            .Include(i => i.Unit).Include(i => i.Food)
            .FirstOrDefaultAsync(i => i.ShoppingListId == listId && i.Id == itemId, ct);
        if (item is null) return null;
        if (request.Note is not null) item.Note = request.Note;
        if (request.Checked.HasValue) item.Checked = request.Checked.Value;
        if (request.DisableAmount.HasValue) item.DisableAmount = request.DisableAmount.Value;
        if (request.Quantity.HasValue) item.Quantity = request.Quantity;
        if (request.UnitId.HasValue) item.UnitId = request.UnitId;
        if (request.FoodId.HasValue) item.FoodId = request.FoodId;
        if (request.LabelId.HasValue) item.LabelId = request.LabelId;
        item.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapItemToResponse(item);
    }

    public async Task<bool> DeleteItemAsync(Guid householdId, Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems
            .FirstOrDefaultAsync(i => i.ShoppingListId == listId && i.Id == itemId, ct);
        if (item is null) return false;
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ShoppingListResponse MapToResponse(ShoppingList s) => new()
    {
        Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId,
        CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt,
        Items = s.Items.Select(MapItemToResponse).ToList(),
    };

    private static ShoppingListItemResponse MapItemToResponse(ShoppingListItem i) => new()
    {
        Id = i.Id, Note = i.Note, IsFood = i.IsFood, Checked = i.Checked,
        DisableAmount = i.DisableAmount, Quantity = i.Quantity,
        ShoppingListId = i.ShoppingListId, UnitId = i.UnitId, FoodId = i.FoodId, LabelId = i.LabelId,
        Position = i.Position, UnitName = i.Unit?.Name, FoodName = i.Food?.Name,
        CreatedAt = i.CreatedAt, UpdateAt = i.UpdateAt,
    };
}
