using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record DeleteStandaloneItemCommand(Guid HouseholdId, Guid ItemId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var item = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && i.Id == ItemId)
            .FirstOrDefaultAsync(ct);
        if (item is null)
        {
            return false;
        }

        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
