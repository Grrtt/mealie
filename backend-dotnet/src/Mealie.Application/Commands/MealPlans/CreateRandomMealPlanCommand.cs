using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record CreateRandomMealPlanCommand(
    Guid GroupId,
    Guid HouseholdId,
    Guid UserId,
    CreateRandomMealPlanRequest Request)
    : IQuery<MealPlanResponse?>
{
    public async Task<MealPlanResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");
        var recipeId =
            await MealPlanRecipeSelectionService.GetRandomRecipeIdAsync(db, logger, GroupId, Request.Date, Request.EntryType,
                ct);
        if (recipeId is null)
        {
            return null;
        }

        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = string.Empty, EntryType = Request.EntryType, Date = Request.Date,
            RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, GroupId, HouseholdId), ct);
        await MealPlanMappingHelper.LoadRecipeNavAsync(db, plan, ct);
        return MealPlanMappingHelper.MapToResponse(plan);
    }
}

