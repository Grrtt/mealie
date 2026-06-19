using Mealie.Application.Dtos.RecipeActions;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.RecipeActions;

public record GetRecipeActionsQuery(Guid HouseholdId) : IQuery<IList<RecipeActionResponse>>
{
    public async Task<IList<RecipeActionResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var actions = await services.Db.RecipeActions.IgnoreQueryFilters()
            .Where(a => a.HouseholdId == HouseholdId).ToListAsync(ct);
        return actions.Select(RecipeActionMappings.MapToResponse).ToList();
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
