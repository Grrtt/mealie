using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record AddRecipeToShoppingListCommand(Guid HouseholdId, Guid ListId, List<AddRecipeToShoppingListRequest> Requests)
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
        if (list is null)
        {
            return null;
        }

        var maxPos = list.Items.Count > 0 ? list.Items.Max(i => i.Position) : -1;
        var position = maxPos + 1;

        foreach (var request in Requests)
        {
            var recipe = await db.Recipes.IgnoreQueryFilters()
                             .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                             .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
                             .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
                         ?? throw new KeyNotFoundException($"Recipe {request.RecipeId} not found");

            var recipeRef = new ShoppingListRecipeReference
            {
                Id = Guid.NewGuid(), ShoppingListId = ListId, RecipeId = request.RecipeId,
                RecipeScale = request.RecipeIncrementQuantity
            };
            db.ShoppingListRecipeReferences.Add(recipeRef);

            // If specific ingredients are provided, filter to only those IDs; otherwise add all ingredients
            var allowedIds = request.RecipeIngredients?.Select(r => r.Id).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();

            foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
            {
                if (allowedIds is { Count: > 0 } && !allowedIds.Contains(ingredient.Id))
                    continue;

                // Merge into an existing item only when both have the same food
                var existingItem = ingredient.FoodId.HasValue
                    ? list.Items.FirstOrDefault(i => i.FoodId == ingredient.FoodId && i.ShoppingListId == ListId)
                    : null;

                if (existingItem != null)
                {
                    if (existingItem.Quantity.HasValue && ingredient.Quantity.HasValue)
                    {
                        existingItem.Quantity += ingredient.Quantity.Value * request.RecipeIncrementQuantity;
                    }

                    existingItem.UpdateAt = DateTime.UtcNow;
                }
                else
                {
                    var isFood = ingredient.FoodId.HasValue;
                    var item = new ShoppingListItem
                    {
                        Id = Guid.NewGuid(), Note = ingredient.Note, IsFood = isFood, Checked = false,
                        DisableAmount = ingredient.DisableAmount,
                        Quantity = ingredient.Quantity * request.RecipeIncrementQuantity,
                        UnitId = ingredient.UnitId, FoodId = ingredient.FoodId,
                        Position = position++, ShoppingListId = ListId,
                        CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                    };
                    db.ShoppingListItems.Add(item);

                    db.ShoppingListItemRecipeReferences.Add(new ShoppingListItemRecipeReference
                    {
                        Id = Guid.NewGuid(), ShoppingListItemId = item.Id, RecipeId = request.RecipeId,
                        RecipeQuantity = ingredient.Quantity ?? 0m, RecipeScale = request.RecipeIncrementQuantity
                    });
                }
            }
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappings.MapToResponse(list);
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
