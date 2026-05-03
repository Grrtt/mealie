using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record CreateShoppingListWithRecipeCommand(
    Guid GroupId,
    Guid HouseholdId,
    Guid UserId,
    CreateShoppingListWithRecipeRequest Request)
    : IQuery<ShoppingListResponse>
{
    public async Task<ShoppingListResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;

        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Name = Request.Name,
            GroupId = GroupId,
            HouseholdId = HouseholdId,
            UserId = UserId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListCreatedEvent(list.Id, GroupId, HouseholdId), ct);

        var recipe = await db.Recipes.IgnoreQueryFilters()
                         .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                         .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
                         .FirstOrDefaultAsync(r => r.Id == Request.RecipeId, ct)
                     ?? throw new KeyNotFoundException("Recipe not found");

        var recipeRef = new ShoppingListRecipeReference
        {
            Id = Guid.NewGuid(),
            ShoppingListId = list.Id,
            RecipeId = Request.RecipeId,
            RecipeScale = Request.RecipeIncrementQuantity
        };
        db.ShoppingListRecipeReferences.Add(recipeRef);

        var position = 0;
        foreach (var ingredient in recipe.RecipeIngredients.OrderBy(i => i.Position))
        {
            var isFood = ingredient.FoodId.HasValue;
            var item = new ShoppingListItem
            {
                Id = Guid.NewGuid(),
                Note = ingredient.Note,
                IsFood = isFood,
                Checked = false,
                DisableAmount = ingredient.DisableAmount,
                Quantity = ingredient.Quantity * Request.RecipeIncrementQuantity,
                UnitId = ingredient.UnitId,
                FoodId = ingredient.FoodId,
                Position = position++,
                ShoppingListId = list.Id,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };
            db.ShoppingListItems.Add(item);

            db.ShoppingListItemRecipeReferences.Add(new ShoppingListItemRecipeReference
            {
                Id = Guid.NewGuid(),
                ShoppingListItemId = item.Id,
                RecipeId = Request.RecipeId,
                RecipeQuantity = ingredient.Quantity ?? 0m,
                RecipeScale = Request.RecipeIncrementQuantity
            });
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await db.Entry(list).Collection(l => l.Items).LoadAsync(ct);
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
}
