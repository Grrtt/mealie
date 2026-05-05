using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.ShoppingLists;

public static class ShoppingListItemMutationService
{
    public static async Task<ShoppingListItemResponse> CreateItemAsync(
        ApplicationDbContext db,
        Guid listId,
        CreateShoppingListItemRequest request,
        bool mergeIfPossible,
        CancellationToken ct = default)
    {
        if (mergeIfPossible && !request.DisableAmount)
        {
            var mergeable = await ShoppingListMappingHelper.WithItemDetails(db.ShoppingListItems)
                .Where(i => i.ShoppingListId == listId && !i.Checked && !i.DisableAmount)
                .ToListAsync(ct);

            var existingItem = mergeable.FirstOrDefault(i => CanMerge(i, request));
            if (existingItem is not null)
            {
                existingItem.Quantity = (existingItem.Quantity ?? 0) + (request.Quantity ?? 1);
                existingItem.UpdateAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return ShoppingListMappingHelper.MapItemToResponse(existingItem);
            }
        }

        var item = CreateItemEntity(listId, request, await GetNextPositionAsync(db, listId, ct));
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
        return ShoppingListMappingHelper.MapItemToResponse(item);
    }

    public static ShoppingListItem CreateItemEntity(Guid listId, CreateShoppingListItemRequest request, int position)
    {
        return new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            Note = request.Note,
            IsFood = request.IsFood,
            DisableAmount = request.DisableAmount,
            Quantity = request.Quantity,
            UnitId = request.UnitId,
            FoodId = request.FoodId,
            LabelId = request.LabelId,
            Position = position,
            ShoppingListId = listId,
            Checked = false,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
    }

    public static void ApplyUpdate(ShoppingListItem item, UpdateShoppingListItemRequest request)
    {
        if (request.Note is not null)
        {
            item.Note = request.Note;
        }

        if (request.Checked.HasValue)
        {
            item.Checked = request.Checked.Value;
        }

        if (request.DisableAmount.HasValue)
        {
            item.DisableAmount = request.DisableAmount.Value;
        }

        if (request.Quantity.HasValue)
        {
            item.Quantity = request.Quantity;
        }

        if (request.UnitId.HasValue)
        {
            item.UnitId = request.UnitId;
        }

        if (request.FoodId.HasValue)
        {
            item.FoodId = request.FoodId;
        }

        if (request.LabelId.HasValue)
        {
            item.LabelId = request.LabelId;
        }

        item.UpdateAt = DateTime.UtcNow;
    }

    public static void ApplyUpdate(ShoppingListItem item, BulkUpdateShoppingListItem request)
    {
        if (request.Note is not null)
        {
            item.Note = request.Note;
        }

        if (request.Checked.HasValue)
        {
            item.Checked = request.Checked.Value;
        }

        if (request.DisableAmount.HasValue)
        {
            item.DisableAmount = request.DisableAmount.Value;
        }

        if (request.Quantity.HasValue)
        {
            item.Quantity = request.Quantity;
        }

        if (request.UnitId.HasValue)
        {
            item.UnitId = request.UnitId;
        }

        if (request.FoodId.HasValue)
        {
            item.FoodId = request.FoodId;
        }

        if (request.LabelId.HasValue)
        {
            item.LabelId = request.LabelId;
        }

        item.UpdateAt = DateTime.UtcNow;
    }

    public static async Task<int> GetNextPositionAsync(ApplicationDbContext db, Guid listId, CancellationToken ct = default)
    {
        var position = await db.ShoppingListItems.Where(i => i.ShoppingListId == listId)
            .MaxAsync(i => (int?)i.Position, ct) ?? -1;
        return position + 1;
    }

    public static bool CanMerge(ShoppingListItem existing, CreateShoppingListItemRequest incoming)
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
