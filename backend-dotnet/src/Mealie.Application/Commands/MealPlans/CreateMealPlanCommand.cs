using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Queries;
using Mealie.Application.Services.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;

namespace Mealie.Application.Commands.MealPlans;

public record CreateMealPlanCommand(Guid GroupId, Guid HouseholdId, Guid UserId, CreateMealPlanRequest Request)
    : IQuery<MealPlanResponse>
{
    public async Task<MealPlanResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = Request.Title, Text = Request.Text,
            EntryType = Request.EntryType, Date = Request.Date, RecipeId = Request.RecipeId,
            GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, GroupId, HouseholdId), ct);
        await MealPlanMappingHelper.LoadRecipeNavAsync(db, plan, ct);
        return MealPlanMappingHelper.MapToResponse(plan);
    }
}
