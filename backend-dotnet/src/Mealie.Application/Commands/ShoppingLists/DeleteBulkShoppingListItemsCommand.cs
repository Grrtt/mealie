using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record DeleteBulkShoppingListItemsCommand(Guid HouseholdId, BulkDeleteShoppingListItemRequest Request) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var items = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Where(i => i.ShoppingList.HouseholdId == HouseholdId && Request.Ids.Contains(i.Id))
            .ToListAsync(ct);
        if (items.Count == 0) return false;
        db.ShoppingListItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
        return true;
    }
}