using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListItemByIdQuery(Guid HouseholdId, Guid ItemId) : IQuery<ShoppingListItemResponse?>
{
    public async Task<ShoppingListItemResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var item = await ShoppingListMappingHelper.WithItemDetails(services.Db.ShoppingListItems).Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && i.Id == ItemId)
            .FirstOrDefaultAsync(ct);
        return item is null ? null : ShoppingListMappingHelper.MapItemToResponse(item);
    }
}

