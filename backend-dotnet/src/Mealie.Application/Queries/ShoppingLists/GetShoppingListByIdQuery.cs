using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListByIdQuery(Guid HouseholdId, Guid Id) : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var list = await ShoppingListMappingHelper.WithItems(services.Db.ShoppingLists.IgnoreQueryFilters())
            .Where(s => s.HouseholdId == HouseholdId && s.Id == Id)
            .FirstOrDefaultAsync(ct);
        return list is null ? null : ShoppingListMappingHelper.MapToResponse(list);
    }
}
