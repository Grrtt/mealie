using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateShoppingListItemCommand(
    Guid HouseholdId,
    Guid ListId,
    Guid ItemId,
    UpdateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse?>
{
    public async Task<ShoppingListItemResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await ShoppingListMappingHelper.WithItemDetails(db.ShoppingListItems)
            .FirstOrDefaultAsync(i => i.ShoppingListId == ListId && i.Id == ItemId, ct);
        if (item is null)
        {
            return null;
        }

        ShoppingListItemMutationService.ApplyUpdate(item, Request);
        await db.SaveChangesAsync(ct);
        return ShoppingListMappingHelper.MapItemToResponse(item);
    }
}

