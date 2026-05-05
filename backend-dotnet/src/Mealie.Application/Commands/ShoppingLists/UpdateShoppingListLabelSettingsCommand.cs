using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.ShoppingLists;

public record UpdateShoppingListLabelSettingsCommand(
    Guid HouseholdId,
    Guid ListId,
    UpdateShoppingListLabelSettingsRequest Request)
    : IQuery<ShoppingListResponse?>
{
    public async Task<ShoppingListResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var list = await ShoppingListMappingHelper.WithItems(db.ShoppingLists.IgnoreQueryFilters())
            .Where(s => s.HouseholdId == HouseholdId && s.Id == ListId)
            .FirstOrDefaultAsync(ct);
        if (list is null)
        {
            return null;
        }

        list.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ShoppingListMappingHelper.MapToResponse(list);
    }
}
