using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record DeleteShoppingListCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await db.ShoppingLists.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == Id, ct);
        if (list is null)
        {
            return false;
        }

        db.ShoppingLists.Remove(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListDeletedEvent(Id, HouseholdId), ct);
        return true;
    }
}
