using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.RecipeActions;

public record DeleteRecipeActionCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var action = await db.RecipeActions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.HouseholdId == HouseholdId && a.Id == Id, ct);
        if (action is null)
        {
            return false;
        }

        db.RecipeActions.Remove(action);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
