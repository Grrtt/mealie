using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record UpdateMealPlanCommand(Guid HouseholdId, Guid Id, UpdateMealPlanRequest Request)
    : IQuery<MealPlanResponse?>
{
    public async Task<MealPlanResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = await MealPlanMappingHelper.WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == HouseholdId && m.Id == Id, ct);
        if (plan is null)
        {
            return null;
        }

        if (Request.Title is not null)
        {
            plan.Title = Request.Title;
        }

        if (Request.Text is not null)
        {
            plan.Text = Request.Text;
        }

        if (Request.EntryType is not null)
        {
            plan.EntryType = Request.EntryType;
        }

        if (Request.Date.HasValue)
        {
            plan.Date = Request.Date.Value;
        }

        if (Request.RecipeId.HasValue)
        {
            plan.RecipeId = Request.RecipeId;
        }

        plan.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryUpdatedEvent(plan.Id, plan.GroupId, HouseholdId), ct);
        await MealPlanMappingHelper.LoadRecipeNavAsync(db, plan, ct);
        return MealPlanMappingHelper.MapToResponse(plan);
    }
}

