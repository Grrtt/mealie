using Mealie.Application.Dtos.RecipeActions;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.RecipeActions;

public record UpdateRecipeActionCommand(Guid HouseholdId, Guid Id, UpdateRecipeActionRequest Request)
    : IQuery<RecipeActionResponse?>
{
    public async Task<RecipeActionResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var action = await db.RecipeActions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.HouseholdId == HouseholdId && a.Id == Id, ct);
        if (action is null)
        {
            return null;
        }

        action.Title = Request.Title;
        action.Url = Request.Url;
        action.ActionType = Request.ActionType;
        action.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return RecipeActionMappings.MapToResponse(action);
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
