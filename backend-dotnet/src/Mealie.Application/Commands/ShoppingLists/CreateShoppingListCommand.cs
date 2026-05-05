using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record CreateShoppingListCommand(Guid GroupId, Guid HouseholdId, Guid UserId, CreateShoppingListRequest Request)
    : IQuery<ShoppingListResponse>
{
    public async Task<ShoppingListResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(), Name = Request.Name, GroupId = GroupId, HouseholdId = HouseholdId,
            UserId = UserId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListCreatedEvent(list.Id, GroupId, HouseholdId), ct);
        return ShoppingListMappingHelper.MapToResponse(list);
    }
}

