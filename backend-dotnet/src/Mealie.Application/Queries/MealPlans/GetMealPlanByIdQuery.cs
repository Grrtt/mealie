using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.MealPlans;

public record GetMealPlanByIdQuery(Guid HouseholdId, Guid Id) : IQuery<MealPlanResponse?>
{
    public async Task<MealPlanResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var m = await MealPlanMappingHelper.WithRecipe(services.Db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == HouseholdId && m.Id == Id, ct);
        return m is null ? null : MealPlanMappingHelper.MapToResponse(m);
    }
}

