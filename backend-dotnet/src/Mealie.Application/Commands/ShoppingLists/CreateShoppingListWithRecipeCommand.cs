using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record CreateShoppingListWithRecipeCommand(
    Guid GroupId,
    Guid HouseholdId,
    Guid UserId,
    CreateShoppingListWithRecipeRequest Request)
    : IQuery<ShoppingListResponse>
{
    public async Task<ShoppingListResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;

        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Name = Request.Name,
            GroupId = GroupId,
            HouseholdId = HouseholdId,
            UserId = UserId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new ShoppingListCreatedEvent(list.Id, GroupId, HouseholdId), ct);

        await ShoppingListRecipeLinkService.AddRecipeToNewListAsync(
            db, list, Request.RecipeId, Request.RecipeIncrementQuantity, ct);

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        list = await ShoppingListMappingHelper.WithItems(db.ShoppingLists.IgnoreQueryFilters())
            .FirstAsync(l => l.Id == list.Id, ct);
        return ShoppingListMappingHelper.MapToResponse(list);
    }
}

