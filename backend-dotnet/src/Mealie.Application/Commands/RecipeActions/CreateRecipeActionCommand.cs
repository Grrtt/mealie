using Mealie.Application.Dtos.RecipeActions;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;

namespace Mealie.Application.Commands.RecipeActions;

public record CreateRecipeActionCommand(Guid GroupId, Guid HouseholdId, CreateRecipeActionRequest Request)
    : IQuery<RecipeActionResponse>
{
    public async Task<RecipeActionResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var action = new RecipeAction
        {
            Id = Guid.NewGuid(),
            Title = Request.Title,
            Url = Request.Url,
            ActionType = Request.ActionType,
            GroupId = GroupId,
            HouseholdId = HouseholdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.RecipeActions.Add(action);
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
