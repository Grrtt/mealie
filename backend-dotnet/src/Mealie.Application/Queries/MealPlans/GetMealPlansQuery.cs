using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.MealPlans;

public record GetMealPlansQuery(Guid HouseholdId, DateOnly? StartDate = null, DateOnly? EndDate = null)
    : IQuery<IList<MealPlanResponse>>
{
    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = MealPlanMappingHelper.WithRecipe(db.MealPlans.IgnoreQueryFilters()
            .Where(m => m.HouseholdId == HouseholdId));
        if (StartDate.HasValue)
        {
            query = query.Where(m => m.Date >= StartDate.Value);
        }

        if (EndDate.HasValue)
        {
            query = query.Where(m => m.Date <= EndDate.Value);
        }

        var plans = await query.OrderBy(m => m.Date).ToListAsync(ct);
        return plans.Select(MealPlanMappingHelper.MapToResponse).ToList();
    }
}

