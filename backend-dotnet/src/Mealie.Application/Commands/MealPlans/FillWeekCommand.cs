using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Commands.MealPlans;

public record FillWeekCommand(Guid GroupId, Guid HouseholdId, Guid UserId, FillWeekRequest Request)
    : IQuery<IList<MealPlanResponse>>
{
    private static readonly IList<string> DefaultFillEntryTypes = ["breakfast", "lunch", "side", "dinner", "side"];

    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");

        var allRules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == GroupId && r.EntryType != "unset")
            .ToListAsync(ct);

        var hasRules = allRules.Count > 0;
        logger.LogDebug("FillWeek: {Rules} rules found for group {Group}", allRules.Count, GroupId);

        var plans = new List<MealPlan>();
        for (var date = Request.StartDate; date <= Request.EndDate; date = date.AddDays(1))
        {
            var dayName = MealPlanRecipeSelectionService.DayOfWeekToRuleDay(date.DayOfWeek);
            IEnumerable<(string entryType, MealPlanRule? rule)> slots;

            if (hasRules)
            {
                var dayRules = allRules.Where(r => r.Day == dayName || r.Day == "unset").ToList();
                slots = dayRules.Select(r => (r.EntryType, (MealPlanRule?)r));
            }
            else
            {
                slots = DefaultFillEntryTypes.Select(t => (t, (MealPlanRule?)null));
            }

            foreach (var (entryType, rule) in slots)
            {
                var recipeId = rule is { Tags.Count: > 0 } or { Categories.Count: > 0 }
                    ? await MealPlanRecipeSelectionService.GetRandomRecipeIdForRuleAsync(db, logger, GroupId, rule, ct)
                    : await MealPlanRecipeSelectionService.GetRandomRecipeIdAsync(db, logger, GroupId, date, entryType, ct);

                if (recipeId is null)
                {
                    continue;
                }

                var plan = new MealPlan
                {
                    Id = Guid.NewGuid(), Title = string.Empty, EntryType = entryType, Date = date,
                    RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.MealPlans.Add(plan);
                plans.Add(plan);
            }
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
