using Mealie.Application.Dtos.RecipeActions;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.RecipeActions;

public record GetRecipeActionByIdQuery(Guid HouseholdId, Guid Id) : IQuery<RecipeActionResponse?>
{
    public async Task<RecipeActionResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var action = await services.Db.RecipeActions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.HouseholdId == HouseholdId && a.Id == Id, ct);
        return action is null ? null : RecipeActionMappings.MapToResponse(action);
    }
}

file static class RecipeActionMappings
{
    public static RecipeActionResponse MapToResponse(RecipeAction a)
    {
        return new RecipeActionResponse
            { Id = a.Id, Title = a.Title, Url = a.Url, ActionType = a.ActionType };
    }
}
