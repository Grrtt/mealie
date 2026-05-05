using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateShoppingListCommand(Guid HouseholdId, Guid Id, UpdateShoppingListRequest Request)
    : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await ShoppingListMappingHelper.WithItems(db.ShoppingLists.IgnoreQueryFilters())
            .Where(s => s.HouseholdId == HouseholdId && s.Id == Id)
            .FirstOrDefaultAsync(ct);
        if (list is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            list.Name = Request.Name;
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListUpdatedEvent(list.Id, list.GroupId, HouseholdId), ct);
        return ShoppingListMappingHelper.MapToResponse(list);
    }
}
