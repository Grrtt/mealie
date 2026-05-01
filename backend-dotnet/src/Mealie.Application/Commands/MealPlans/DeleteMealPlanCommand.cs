using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record DeleteMealPlanCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = await db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.HouseholdId == HouseholdId && m.Id == Id, ct);
        if (plan is null)
        {
            return false;
        }

        db.MealPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryDeletedEvent(Id, HouseholdId), ct);
        return true;
    }
}
