using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record CreateStandaloneItemCommand(Guid HouseholdId, Guid ListId, CreateShoppingListItemRequest Request)
    : IQuery<ShoppingListItemResponse>
{
    public async Task<ShoppingListItemResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        _ = await db.ShoppingLists.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.HouseholdId == HouseholdId && s.Id == ListId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");
        return await ShoppingListItemMutationService.CreateItemAsync(db, ListId, Request, true, ct);
    }
}

