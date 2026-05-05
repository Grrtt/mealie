using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateBulkShoppingListItemsCommand(Guid HouseholdId, BulkUpdateShoppingListItemRequest Request)
    : IQuery<IList<ShoppingListItemResponse>>
{
    public async Task<IList<ShoppingListItemResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var itemIds = Request.Items.Select(i => i.Id).ToList();
        var items = await ShoppingListMappingHelper.WithItemDetails(db.ShoppingListItems).Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && itemIds.Contains(i.Id))
            .ToListAsync(ct);

        var responses = new List<ShoppingListItemResponse>();
        foreach (var req in Request.Items)
        {
            var item = items.FirstOrDefault(i => i.Id == req.Id);
            if (item is null)
            {
                continue;
            }

            ShoppingListItemMutationService.ApplyUpdate(item, req);
            responses.Add(ShoppingListMappingHelper.MapItemToResponse(item));
        }

        await db.SaveChangesAsync(ct);
        return responses;
    }
}

