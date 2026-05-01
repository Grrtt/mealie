using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record DeleteMealPlanRuleCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var rule = await db.MealPlanRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.GroupId == GroupId && r.Id == Id, ct);
        if (rule is null) return false;
        db.MealPlanRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        return true;
    }
}