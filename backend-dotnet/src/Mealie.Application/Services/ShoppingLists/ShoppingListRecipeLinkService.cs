using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.ShoppingLists;

public static class ShoppingListRecipeLinkService
{
    public static async Task AddRecipeToNewListAsync(
        ApplicationDbContext db,
        ShoppingList list,
        Guid recipeId,
        decimal recipeIncrementQuantity,
        CancellationToken ct = default)
    {
        var recipe = await LoadRecipeAsync(db, recipeId, ct);
        db.ShoppingListRecipeReferences.Add(new ShoppingListRecipeReference
        {
            Id = Guid.NewGuid(),
            ShoppingListId = list.Id,
            RecipeId = recipeId,
            RecipeScale = recipeIncrementQuantity
        });

        var position = list.Items.Count;
        foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
        {
            var item = ShoppingListItemMutationService.CreateItemEntity(
                list.Id,
                CreateItemRequest(ingredient.Note, ingredient.FoodId.HasValue, ingredient.DisableAmount,
                    ingredient.Quantity * recipeIncrementQuantity, ingredient.UnitId, ingredient.FoodId, null),
                position++);
            db.ShoppingListItems.Add(item);
            list.Items.Add(item);

            db.ShoppingListItemRecipeReferences.Add(new ShoppingListItemRecipeReference
            {
                Id = Guid.NewGuid(),
                ShoppingListItemId = item.Id,
                RecipeId = recipeId,
                RecipeQuantity = ingredient.Quantity ?? 0m,
                RecipeScale = recipeIncrementQuantity
            });
        }
    }

    public static async Task AddRecipesAsync(
        ApplicationDbContext db,
        ShoppingList list,
        IList<AddRecipeToShoppingListRequest> requests,
        CancellationToken ct = default)
    {
        var position = list.Items.Count > 0 ? list.Items.Max(i => i.Position) + 1 : 0;
        foreach (var request in requests)
        {
            var recipe = await LoadRecipeAsync(db, request.RecipeId, ct);
            db.ShoppingListRecipeReferences.Add(new ShoppingListRecipeReference
            {
                Id = Guid.NewGuid(),
                ShoppingListId = list.Id,
                RecipeId = request.RecipeId,
                RecipeScale = request.RecipeIncrementQuantity
            });

            var allowedIds = request.RecipeIngredients?
                .Select(r => r.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
            {
                if (allowedIds is { Count: > 0 } && !allowedIds.Contains(ingredient.Id))
                {
                    continue;
                }

                var existingItem = ingredient.FoodId.HasValue
                    ? list.Items.FirstOrDefault(i => i.FoodId == ingredient.FoodId && i.ShoppingListId == list.Id)
                    : null;

                if (existingItem is not null)
                {
                    if (existingItem.Quantity.HasValue && ingredient.Quantity.HasValue)
                    {
                        existingItem.Quantity += ingredient.Quantity.Value * request.RecipeIncrementQuantity;
                    }

                    existingItem.UpdateAt = DateTime.UtcNow;
                    continue;
                }

                var item = ShoppingListItemMutationService.CreateItemEntity(
                    list.Id,
                    CreateItemRequest(ingredient.Note, ingredient.FoodId.HasValue, ingredient.DisableAmount,
                        ingredient.Quantity * request.RecipeIncrementQuantity, ingredient.UnitId, ingredient.FoodId, null),
                    position++);
                db.ShoppingListItems.Add(item);
                list.Items.Add(item);

                db.ShoppingListItemRecipeReferences.Add(new ShoppingListItemRecipeReference
                {
                    Id = Guid.NewGuid(),
                    ShoppingListItemId = item.Id,
                    RecipeId = request.RecipeId,
                    RecipeQuantity = ingredient.Quantity ?? 0m,
                    RecipeScale = request.RecipeIncrementQuantity
                });
            }
        }
    }

    private static CreateShoppingListItemRequest CreateItemRequest(
        string? note,
        bool isFood,
        bool disableAmount,
        decimal? quantity,
        Guid? unitId,
        Guid? foodId,
        Guid? labelId)
    {
        return new CreateShoppingListItemRequest
        {
            Note = note,
            IsFood = isFood,
            DisableAmount = disableAmount,
            Quantity = quantity,
            UnitId = unitId,
            FoodId = foodId,
            LabelId = labelId
        };
    }

    private static async Task<Domain.Entities.Recipes.Recipe> LoadRecipeAsync(
        ApplicationDbContext db,
        Guid recipeId,
        CancellationToken ct)
    {
        return await db.Recipes.IgnoreQueryFilters()
                   .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                   .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
                   .FirstOrDefaultAsync(r => r.Id == recipeId, ct)
               ?? throw new KeyNotFoundException($"Recipe {recipeId} not found");
    }
}
