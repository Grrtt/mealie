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

    // ── Standalone Items API ─────────────────────────────────────────────────

    public async Task<PaginatedResponse<ShoppingListItemResponse>> GetItemsAsync(Guid householdId, PaginationParams pagination, bool? checked_ = null, CancellationToken ct = default)
    {
        var query = db.ShoppingListItems
            .Include(i => i.Unit)
            .Include(i => i.Food)
            .Where(i => i.ShoppingList.HouseholdId == householdId);

        if (checked_.HasValue)
            query = query.Where(i => i.Checked == checked_.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.UpdateAt).ThenBy(i => i.Position)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(i => MapItemToResponse(i))
            .ToListAsync(ct);
        return new PaginatedResponse<ShoppingListItemResponse> 
        { 
            Page = pagination.Page, 
            PerPage = pagination.PerPage, 
            Total = total, 
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), 
            Items = items 
        };
    }

    public async Task<ShoppingListItemResponse?> GetItemByIdAsync(Guid householdId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems
            .Include(i => i.Unit)
            .Include(i => i.Food)
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == householdId && i.Id == itemId)
            .FirstOrDefaultAsync(ct);
        if (item is null) return null;
        return MapItemToResponse(item);
    }

    public async Task<ShoppingListItemResponse> CreateStandaloneItemAsync(Guid householdId, CreateShoppingListItemRequest request, Guid listId, CancellationToken ct = default)
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

    public async Task<ShoppingListItemResponse?> UpdateStandaloneItemAsync(Guid householdId, Guid itemId, UpdateShoppingListItemRequest request, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems
            .Include(i => i.Unit)
            .Include(i => i.Food)
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == householdId && i.Id == itemId)
            .FirstOrDefaultAsync(ct);
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

    public async Task<bool> DeleteStandaloneItemAsync(Guid householdId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == householdId && i.Id == itemId)
            .FirstOrDefaultAsync(ct);
        if (item is null) return false;
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<ShoppingListItemResponse>> CreateBulkItemsAsync(Guid householdId, BulkCreateShoppingListItemRequest request, CancellationToken ct = default)
    {
        var listIds = request.Items.Select(i => i.ListId).Distinct().ToList();
        var lists = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId && listIds.Contains(s.Id))
            .ToListAsync(ct);

        if (lists.Count != listIds.Count)
            throw new KeyNotFoundException("One or more shopping lists not found");

        var items = new List<ShoppingListItem>();
        var responses = new List<ShoppingListItemResponse>();

        foreach (var req in request.Items)
        {
            var position = await db.ShoppingListItems
                .Where(i => i.ShoppingListId == req.ListId)
                .MaxAsync(i => (int?)i.Position, ct) ?? -1;

            var item = new ShoppingListItem
            {
                Id = Guid.NewGuid(), Note = req.Note, IsFood = req.IsFood,
                DisableAmount = req.DisableAmount, Quantity = req.Quantity,
                UnitId = req.UnitId, FoodId = req.FoodId, LabelId = req.LabelId,
                Position = position + 1, ShoppingListId = req.ListId, Checked = false,
                CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
            };
            items.Add(item);
            responses.Add(MapItemToResponse(item));
        }

        db.ShoppingListItems.AddRange(items);
        await db.SaveChangesAsync(ct);
        return responses;
    }

    public async Task<IList<ShoppingListItemResponse>> UpdateBulkItemsAsync(Guid householdId, BulkUpdateShoppingListItemRequest request, CancellationToken ct = default)
    {
        var itemIds = request.Items.Select(i => i.Id).ToList();
        var items = await db.ShoppingListItems
            .Include(i => i.Unit)
            .Include(i => i.Food)
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == householdId && itemIds.Contains(i.Id))
            .ToListAsync(ct);

        var responses = new List<ShoppingListItemResponse>();
        foreach (var req in request.Items)
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
            responses.Add(MapItemToResponse(item));
        }

        await db.SaveChangesAsync(ct);
        return responses;
    }

    public async Task<bool> DeleteBulkItemsAsync(Guid householdId, BulkDeleteShoppingListItemRequest request, CancellationToken ct = default)
    {
        var items = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == householdId && request.Ids.Contains(i.Id))
            .ToListAsync(ct);

        if (items.Count == 0) return false;
        db.ShoppingListItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Recipe Linking ───────────────────────────────────────────────────────

    public async Task<ShoppingListResponse?> AddRecipeAsync(Guid householdId, Guid listId, AddRecipeToShoppingListRequest request, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId && s.Id == listId)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct);
        if (recipe is null) throw new KeyNotFoundException("Recipe not found");

        // Create recipe reference
        var recipeRef = new ShoppingListRecipeReference
        {
            Id = Guid.NewGuid(),
            ShoppingListId = listId,
            RecipeId = request.RecipeId,
            RecipeScale = request.RecipeIncrementQuantity,
        };
        db.ShoppingListRecipeReferences.Add(recipeRef);

        // Add ingredients from recipe
        var maxPos = list.Items.Count > 0 ? list.Items.Max(i => i.Position) : -1;
        var position = maxPos + 1;

        foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
        {
            if (!ingredient.IsFood) continue; // Only add food items

            // Check if item with same food already exists in list
            var existingItem = list.Items.FirstOrDefault(i => i.FoodId == ingredient.FoodId && i.ShoppingListId == listId);
            if (existingItem != null)
            {
                // Merge: add quantities
                if (existingItem.Quantity.HasValue && ingredient.Quantity.HasValue)
                    existingItem.Quantity += ingredient.Quantity.Value * request.RecipeIncrementQuantity;
                existingItem.UpdateAt = DateTime.UtcNow;
            }
            else
            {
                // Create new item
                var item = new ShoppingListItem
                {
                    Id = Guid.NewGuid(),
                    Note = ingredient.Title,
                    IsFood = true,
                    Checked = false,
                    DisableAmount = ingredient.DisableAmount,
                    Quantity = ingredient.Quantity * request.RecipeIncrementQuantity,
                    UnitId = ingredient.UnitId,
                    FoodId = ingredient.FoodId,
                    Position = position++,
                    ShoppingListId = listId,
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                };
                db.ShoppingListItems.Add(item);

                // Create recipe reference for this item
                var itemRecipeRef = new ShoppingListItemRecipeReference
                {
                    Id = Guid.NewGuid(),
                    ShoppingListItemId = item.Id,
                    RecipeId = request.RecipeId,
                    RecipeQuantity = ingredient.Quantity ?? 0m,
                    RecipeScale = request.RecipeIncrementQuantity,
                };
                db.ShoppingListItemRecipeReferences.Add(itemRecipeRef);
            }
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(list);
    }

    public async Task<bool> RemoveRecipeAsync(Guid householdId, Guid listId, Guid recipeId, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == householdId && s.Id == listId, ct);
        if (list is null) return false;

        // Find all items in this list that reference this recipe
        var itemRefs = await db.ShoppingListItemRecipeReferences
            .Where(r => r.Recipe.Id == recipeId && r.ShoppingListItem.ShoppingListId == listId)
            .Include(r => r.ShoppingListItem)
            .ToListAsync(ct);

        if (itemRefs.Count == 0) return false;

        // Delete item references first
        db.ShoppingListItemRecipeReferences.RemoveRange(itemRefs);

        // Delete items that only have this recipe reference (no other refs)
        var itemIds = itemRefs.Select(r => r.ShoppingListItemId).Distinct().ToList();
        var itemsToDelete = new List<ShoppingListItem>();
        
        foreach (var itemId in itemIds)
        {
            var otherRefs = await db.ShoppingListItemRecipeReferences
                .CountAsync(r => r.ShoppingListItemId == itemId, ct);
            if (otherRefs == 0)
            {
                var item = await db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
                if (item != null)
                    itemsToDelete.Add(item);
            }
        }

        db.ShoppingListItems.RemoveRange(itemsToDelete);

        // Delete list-level recipe reference
        var listRecipeRef = await db.ShoppingListRecipeReferences
            .FirstOrDefaultAsync(r => r.ShoppingListId == listId && r.RecipeId == recipeId, ct);
        if (listRecipeRef != null)
            db.ShoppingListRecipeReferences.Remove(listRecipeRef);

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShoppingListResponse?> UpdateLabelSettingsAsync(Guid householdId, Guid listId, UpdateShoppingListLabelSettingsRequest request, CancellationToken ct = default)
    {
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .Where(s => s.HouseholdId == householdId && s.Id == listId)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .Include(s => s.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(ct);
        if (list is null) return null;

        if (request.LabelSettings is not null)
        {
            // Store label settings if needed (could be JSON in a field on ShoppingList)
            // For now, just update the timestamp
        }
        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(list);
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
