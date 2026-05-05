using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateStandaloneItemCommand(Guid HouseholdId, Guid ItemId, UpdateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse?>
{
    public async Task<ShoppingListItemResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await ShoppingListMappingHelper.WithItemDetails(db.ShoppingListItems).Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && i.Id == ItemId)
            .FirstOrDefaultAsync(ct);
        if (item is null)
        {
            return null;
        }

        ShoppingListItemMutationService.ApplyUpdate(item, Request);
        await db.SaveChangesAsync(ct);
        return ShoppingListMappingHelper.MapItemToResponse(item);
    }
}

