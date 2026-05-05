using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.MealPlans;

public record GetTodayMealPlansQuery(Guid HouseholdId) : IQuery<IList<MealPlanResponse>>
{
    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plans = await MealPlanMappingHelper.WithRecipe(services.Db.MealPlans.IgnoreQueryFilters()
                .Where(m => m.HouseholdId == HouseholdId && m.Date == today))
            .ToListAsync(ct);
        return plans.Select(MealPlanMappingHelper.MapToResponse).ToList();
    }
}

