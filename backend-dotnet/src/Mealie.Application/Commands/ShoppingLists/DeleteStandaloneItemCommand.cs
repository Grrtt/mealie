using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
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
        if (item is null) return false;
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}