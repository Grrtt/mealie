using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record CreateShoppingListCommand(Guid GroupId, Guid HouseholdId, CreateShoppingListRequest Request)
    : IQuery<ShoppingListResponse>
{
    public async Task<ShoppingListResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(), Name = Request.Name, GroupId = GroupId, HouseholdId = HouseholdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListCreatedEvent(list.Id, GroupId, HouseholdId), ct);
        return ShoppingListMappings.MapToResponse(list);
    }
}

public record UpdateShoppingListCommand(Guid HouseholdId, Guid Id, UpdateShoppingListRequest Request)
    : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && s.Id == Id)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;
        if (Request.Name is not null) list.Name = Request.Name;
        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListUpdatedEvent(list.Id, list.GroupId, HouseholdId), ct);
        return ShoppingListMappings.MapToResponse(list);
    }
}

public record DeleteShoppingListCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == Id, ct);
        if (list is null) return false;
        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListDeletedEvent(Id, HouseholdId), ct);
        return true;
    }
}

public record AddShoppingListItemCommand(Guid HouseholdId, Guid ListId, CreateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse>
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        _ = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == ListId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");

        var position = await db.ShoppingListItems.Where(i => i.ShoppingListId == ListId)
            .MaxAsync(i => (int?)i.Position, ct) ?? -1;
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(), Note = Request.Note, IsFood = Request.IsFood,
            DisableAmount = Request.DisableAmount, Quantity = Request.Quantity,
            UnitId = Request.UnitId, FoodId = Request.FoodId, LabelId = Request.LabelId,
            Position = position + 1, ShoppingListId = ListId, Checked = false,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapItemToResponse(item);
    }
}

public record UpdateShoppingListItemCommand(Guid HouseholdId, Guid ListId, Guid ItemId, UpdateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse?>
{
    public async Task<ShoppingListItemResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await db.ShoppingListItems
            .Include(i => i.Unit).Include(i => i.Food)
            .FirstOrDefaultAsync(i => i.ShoppingListId == ListId && i.Id == ItemId, ct);
        if (item is null) return null;

        if (Request.Note is not null) item.Note = Request.Note;
        if (Request.Checked.HasValue) item.Checked = Request.Checked.Value;
        if (Request.DisableAmount.HasValue) item.DisableAmount = Request.DisableAmount.Value;
        if (Request.Quantity.HasValue) item.Quantity = Request.Quantity;
        if (Request.UnitId.HasValue) item.UnitId = Request.UnitId;
        if (Request.FoodId.HasValue) item.FoodId = Request.FoodId;
        if (Request.LabelId.HasValue) item.LabelId = Request.LabelId;

        item.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapItemToResponse(item);
    }
}

public record DeleteShoppingListItemCommand(Guid HouseholdId, Guid ListId, Guid ItemId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await db.ShoppingListItems.FirstOrDefaultAsync(i => i.ShoppingListId == ListId && i.Id == ItemId, ct);
        if (item is null) return false;
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record CreateStandaloneItemCommand(Guid HouseholdId, Guid ListId, CreateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse>
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        _ = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == ListId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");
        return await ShoppingListMappings.CreateItemWithMergeAsync(db, ListId, Request, ct);
    }
}

public record UpdateStandaloneItemCommand(Guid HouseholdId, Guid ItemId, UpdateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse?>
{
    public async Task<ShoppingListItemResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await db.ShoppingListItems
            .Include(i => i.Unit).Include(i => i.Food).Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && i.Id == ItemId)
            .FirstOrDefaultAsync(ct);
        if (item is null) return null;

        if (Request.Note is not null) item.Note = Request.Note;
        if (Request.Checked.HasValue) item.Checked = Request.Checked.Value;
        if (Request.DisableAmount.HasValue) item.DisableAmount = Request.DisableAmount.Value;
        if (Request.Quantity.HasValue) item.Quantity = Request.Quantity;
        if (Request.UnitId.HasValue) item.UnitId = Request.UnitId;
        if (Request.FoodId.HasValue) item.FoodId = Request.FoodId;
        if (Request.LabelId.HasValue) item.LabelId = Request.LabelId;

        item.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapItemToResponse(item);
    }
}

public record DeleteStandaloneItemCommand(Guid HouseholdId, Guid ItemId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && i.Id == ItemId)
            .FirstOrDefaultAsync(ct);
        if (item is null) return false;
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record CreateBulkShoppingListItemsCommand(Guid HouseholdId, BulkCreateShoppingListItemRequest Request)
    : IQuery<IList<ShoppingListItemResponse>>
{
    public async Task<IList<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var listIds = Request.Items.Select(i => i.ListId).Distinct().ToList();
        var lists = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && listIds.Contains(s.Id)).ToListAsync(ct);
        if (lists.Count != listIds.Count)
            throw new KeyNotFoundException("One or more shopping lists not found");

        var responses = new List<ShoppingListItemResponse>();
        foreach (var req in Request.Items)
        {
            var itemRequest = new CreateShoppingListItemRequest
            {
                Note = req.Note, IsFood = req.IsFood, DisableAmount = req.DisableAmount,
                Quantity = req.Quantity, UnitId = req.UnitId, FoodId = req.FoodId, LabelId = req.LabelId
            };
            responses.Add(await ShoppingListMappings.CreateItemWithMergeAsync(db, req.ListId, itemRequest, ct));
        }
        return responses;
    }
}

public record UpdateBulkShoppingListItemsCommand(Guid HouseholdId, BulkUpdateShoppingListItemRequest Request)
    : IQuery<IList<ShoppingListItemResponse>>
{
    public async Task<IList<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
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
            if (item is null) continue;

            if (req.Note is not null) item.Note = req.Note;
            if (req.Checked.HasValue) item.Checked = req.Checked.Value;
            if (req.DisableAmount.HasValue) item.DisableAmount = req.DisableAmount.Value;
            if (req.Quantity.HasValue) item.Quantity = req.Quantity;
            if (req.UnitId.HasValue) item.UnitId = req.UnitId;
            if (req.FoodId.HasValue) item.FoodId = req.FoodId;
            if (req.LabelId.HasValue) item.LabelId = req.LabelId;

            item.UpdateAt = DateTime.UtcNow;
            responses.Add(ShoppingListMappings.MapItemToResponse(item));
        }
        await db.SaveChangesAsync(ct);
        return responses;
    }
}

public record DeleteBulkShoppingListItemsCommand(Guid HouseholdId, BulkDeleteShoppingListItemRequest Request) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var items = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && Request.Ids.Contains(i.Id))
            .ToListAsync(ct);
        if (items.Count == 0) return false;
        db.ShoppingListItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record AddRecipeToShoppingListCommand(Guid HouseholdId, Guid ListId, AddRecipeToShoppingListRequest Request)
    : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && s.Id == ListId)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
            .FirstOrDefaultAsync(r => r.Id == Request.RecipeId, ct)
            ?? throw new KeyNotFoundException("Recipe not found");

        var recipeRef = new ShoppingListRecipeReference
        {
            Id = Guid.NewGuid(), ShoppingListId = ListId, RecipeId = Request.RecipeId,
            RecipeScale = Request.RecipeIncrementQuantity
        };
        db.ShoppingListRecipeReferences.Add(recipeRef);

        var maxPos = list.Items.Count > 0 ? list.Items.Max(i => i.Position) : -1;
        var position = maxPos + 1;

        foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
        {
            if (!ingredient.IsFood) continue;

            var existingItem = list.Items.FirstOrDefault(i => i.FoodId == ingredient.FoodId && i.ShoppingListId == ListId);
            if (existingItem != null)
            {
                if (existingItem.Quantity.HasValue && ingredient.Quantity.HasValue)
                    existingItem.Quantity += ingredient.Quantity.Value * Request.RecipeIncrementQuantity;
                existingItem.UpdateAt = DateTime.UtcNow;
            }
            else
            {
                var item = new ShoppingListItem
                {
                    Id = Guid.NewGuid(), Note = ingredient.Title, IsFood = true, Checked = false,
                    DisableAmount = ingredient.DisableAmount,
                    Quantity = ingredient.Quantity * Request.RecipeIncrementQuantity,
                    UnitId = ingredient.UnitId, FoodId = ingredient.FoodId,
                    Position = position++, ShoppingListId = ListId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.ShoppingListItems.Add(item);

                db.ShoppingListItemRecipeReferences.Add(new ShoppingListItemRecipeReference
                {
                    Id = Guid.NewGuid(), ShoppingListItemId = item.Id, RecipeId = Request.RecipeId,
                    RecipeQuantity = ingredient.Quantity ?? 0m, RecipeScale = Request.RecipeIncrementQuantity
                });
            }
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapToResponse(list);
    }
}

public record RemoveRecipeFromShoppingListCommand(Guid HouseholdId, Guid ListId, Guid RecipeId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == ListId, ct);
        if (list is null) return false;

        var itemRefs = await db.ShoppingListItemRecipeReferences
            .Where(r => r.Recipe.Id == RecipeId && r.ShoppingListItem.ShoppingListId == ListId)
            .Include(r => r.ShoppingListItem)
            .ToListAsync(ct);
        if (itemRefs.Count == 0) return false;

        db.ShoppingListItemRecipeReferences.RemoveRange(itemRefs);

        var itemIds = itemRefs.Select(r => r.ShoppingListItemId).Distinct().ToList();
        var itemsToDelete = new List<ShoppingListItem>();
        foreach (var itemId in itemIds)
        {
            var otherRefs = await db.ShoppingListItemRecipeReferences.CountAsync(r => r.ShoppingListItemId == itemId, ct);
            if (otherRefs == 0)
            {
                var item = await db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
                if (item != null) itemsToDelete.Add(item);
            }
        }
        db.ShoppingListItems.RemoveRange(itemsToDelete);

        var listRecipeRef = await db.ShoppingListRecipeReferences
            .FirstOrDefaultAsync(r => r.ShoppingListId == ListId && r.RecipeId == RecipeId, ct);
        if (listRecipeRef != null) db.ShoppingListRecipeReferences.Remove(listRecipeRef);

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record UpdateShoppingListLabelSettingsCommand(Guid HouseholdId, Guid ListId, UpdateShoppingListLabelSettingsRequest Request)
    : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == HouseholdId && s.Id == ListId)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;
        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapToResponse(list);
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
