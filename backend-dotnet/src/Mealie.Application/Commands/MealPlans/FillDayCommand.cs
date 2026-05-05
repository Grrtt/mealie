using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record FillDayCommand(Guid GroupId, Guid HouseholdId, Guid UserId, FillDayRequest Request)
    : IQuery<IList<MealPlanResponse>>
{
    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");
        var plans = new List<MealPlan>();
        foreach (var entryType in Request.EntryTypes)
        {
            var recipeId =
                await MealPlanRecipeSelectionService.GetRandomRecipeIdAsync(db, logger, GroupId, Request.Date, entryType, ct);
            if (recipeId is null)
            {
                continue;
            }

            var plan = new MealPlan
            {
                Id = Guid.NewGuid(), Title = string.Empty, EntryType = entryType, Date = Request.Date,
                RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
                CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
            };
            db.MealPlans.Add(plan);
            plans.Add(plan);
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans)
        {
            await MealPlanMappingHelper.LoadRecipeNavAsync(db, plan, ct);
            await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, plan.GroupId, plan.HouseholdId), ct);
        }

        return plans.Select(MealPlanMappingHelper.MapToResponse).ToList();
    }
}

