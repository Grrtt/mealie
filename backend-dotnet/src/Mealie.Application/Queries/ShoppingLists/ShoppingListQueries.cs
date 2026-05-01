using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
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

public record GetShoppingListByIdQuery(Guid HouseholdId, Guid Id) : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var list = await services.Db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && s.Id == Id)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        return list is null ? null : ShoppingListMappings.MapToResponse(list);
    }
}

file static class ShoppingListMappings
{
    public static ShoppingListResponse MapToResponse(ShoppingList s) =>
        new()
        {
            Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId,
            CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt,
            Items = s.Items.Select(MapItemToResponse).ToList()
        };

    public static ShoppingListItemResponse MapItemToResponse(ShoppingListItem i) =>
        new()
        {
            Id = i.Id, Note = i.Note, IsFood = i.IsFood, Checked = i.Checked,
            DisableAmount = i.DisableAmount, Quantity = i.Quantity,
            ShoppingListId = i.ShoppingListId, UnitId = i.UnitId, FoodId = i.FoodId, LabelId = i.LabelId,
            Position = i.Position, UnitName = i.Unit?.Name, FoodName = i.Food?.Name,
            CreatedAt = i.CreatedAt, UpdateAt = i.UpdateAt
        };

    public static async Task<ShoppingListItemResponse> CreateItemWithMergeAsync(
        ApplicationDbContext db, Guid listId, CreateShoppingListItemRequest request, CancellationToken ct)
    {
        if (!request.DisableAmount)
        {
            var candidates = await db.ShoppingListItems
                .Include(i => i.Unit).Include(i => i.Food)
                .Where(i => i.ShoppingListId == listId && !i.Checked && !i.DisableAmount)
                .ToListAsync(ct);

            var mergeable = candidates.FirstOrDefault(i => CanMerge(i, request));
            if (mergeable is not null)
            {
                mergeable.Quantity = (mergeable.Quantity ?? 0) + (request.Quantity ?? 1);
                mergeable.UpdateAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return MapItemToResponse(mergeable);
            }
        }

        var position = await db.ShoppingListItems.Where(i => i.ShoppingListId == listId)
            .MaxAsync(i => (int?)i.Position, ct) ?? -1;
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(), Note = request.Note, IsFood = request.IsFood,
            DisableAmount = request.DisableAmount, Quantity = request.Quantity,
            UnitId = request.UnitId, FoodId = request.FoodId, LabelId = request.LabelId,
            Position = position + 1, ShoppingListId = listId, Checked = false,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
        return MapItemToResponse(item);
    }

    private static bool CanMerge(ShoppingListItem existing, CreateShoppingListItemRequest incoming)
    {
        if (existing.DisableAmount || incoming.DisableAmount) return false;
        if (incoming.FoodId.HasValue && existing.FoodId.HasValue)
            return existing.FoodId == incoming.FoodId && existing.UnitId == incoming.UnitId;
        if (!incoming.FoodId.HasValue && !existing.FoodId.HasValue)
            return !string.IsNullOrEmpty(existing.Note) && existing.Note == incoming.Note;
        return false;
    }
}
