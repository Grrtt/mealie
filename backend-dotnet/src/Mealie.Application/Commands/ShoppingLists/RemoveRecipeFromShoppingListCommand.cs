using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record RemoveRecipeFromShoppingListCommand(Guid HouseholdId, Guid ListId, Guid RecipeId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == ListId, ct);
        if (list is null)
        {
            return false;
        }

        var itemRefs = await db.ShoppingListItemRecipeReferences
            .Where(r => r.Recipe.Id == RecipeId && r.ShoppingListItem.ShoppingListId == ListId)
            .Include(r => r.ShoppingListItem)
            .ToListAsync(ct);
        if (itemRefs.Count == 0)
        {
            return false;
        }

        db.ShoppingListItemRecipeReferences.RemoveRange(itemRefs);

        var itemIds = itemRefs.Select(r => r.ShoppingListItemId).Distinct().ToList();
        var itemsToDelete = new List<ShoppingListItem>();
        foreach (var itemId in itemIds)
        {
            var otherRefs =
                await db.ShoppingListItemRecipeReferences.CountAsync(r => r.ShoppingListItemId == itemId, ct);
            if (otherRefs == 0)
            {
                var item = await db.ShoppingListItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
                if (item != null)
                {
                    itemsToDelete.Add(item);
                }
            }
        }

        db.ShoppingListItems.RemoveRange(itemsToDelete);

        var listRecipeRef = await db.ShoppingListRecipeReferences
            .FirstOrDefaultAsync(r => r.ShoppingListId == ListId && r.RecipeId == RecipeId, ct);
        if (listRecipeRef != null)
        {
            db.ShoppingListRecipeReferences.Remove(listRecipeRef);
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
