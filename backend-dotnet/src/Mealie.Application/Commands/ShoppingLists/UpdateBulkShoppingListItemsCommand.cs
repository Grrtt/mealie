using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateBulkShoppingListItemsCommand(Guid HouseholdId, BulkUpdateShoppingListItemRequest Request)
    : IQuery<IList<ShoppingListItemResponse>>
{
    public async Task<IList<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var itemIds = Request.Items.Select(i => i.Id).ToList();
        var items = await db.ShoppingListItems
            .Include(i => i.Unit).Include(i => i.Food).Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && itemIds.Contains(i.Id))
            .ToListAsync(ct);

        var responses = new List<ShoppingListItemResponse>();
        foreach (var req in Request.Items)
        {
            var item = items.FirstOrDefault(i => i.Id == req.Id);
            if (item is null)
            {
                continue;
            }

            if (req.Note is not null)
            {
                item.Note = req.Note;
            }

            if (req.Checked.HasValue)
            {
                item.Checked = req.Checked.Value;
            }

            if (req.DisableAmount.HasValue)
            {
                item.DisableAmount = req.DisableAmount.Value;
            }

            if (req.Quantity.HasValue)
            {
                item.Quantity = req.Quantity;
            }

            if (req.UnitId.HasValue)
            {
                item.UnitId = req.UnitId;
            }

            if (req.FoodId.HasValue)
            {
                item.FoodId = req.FoodId;
            }

            if (req.LabelId.HasValue)
            {
                item.LabelId = req.LabelId;
            }

            item.UpdateAt = DateTime.UtcNow;
            responses.Add(ShoppingListMappings.MapItemToResponse(item));
        }

        await db.SaveChangesAsync(ct);
        return responses;
    }
}

file static class ShoppingListMappings
{
    public static ShoppingListResponse MapToResponse(ShoppingList s)
    {
        return new ShoppingListResponse
        {
            Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId,
            CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt,
            Items = s.Items.Select(MapItemToResponse).ToList()
        };
    }

    public static ShoppingListItemResponse MapItemToResponse(ShoppingListItem i)
    {
        return new ShoppingListItemResponse
        {
            Id = i.Id, Note = i.Note, IsFood = i.IsFood, Checked = i.Checked,
            DisableAmount = i.DisableAmount, Quantity = i.Quantity,
            ShoppingListId = i.ShoppingListId, UnitId = i.UnitId, FoodId = i.FoodId, LabelId = i.LabelId,
            Position = i.Position, UnitName = i.Unit?.Name, FoodName = i.Food?.Name,
            CreatedAt = i.CreatedAt, UpdateAt = i.UpdateAt
        };
    }

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
        if (existing.DisableAmount || incoming.DisableAmount)
        {
            return false;
        }

        if (incoming.FoodId.HasValue && existing.FoodId.HasValue)
        {
            return existing.FoodId == incoming.FoodId && existing.UnitId == incoming.UnitId;
        }

        if (!incoming.FoodId.HasValue && !existing.FoodId.HasValue)
        {
            return !string.IsNullOrEmpty(existing.Note) && existing.Note == incoming.Note;
        }

        return false;
    }
}
