using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.MealPlans;

public record GetMealPlanRulesQuery(Guid GroupId, Guid HouseholdId) : IQuery<IList<MealPlanRuleResponse>>
{
    public async Task<IList<MealPlanRuleResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var rules = await RuleHelpers.WithNav(services.Db.MealPlanRules.IgnoreQueryFilters())
            .Where(r => r.GroupId == GroupId && (r.HouseholdId == null || r.HouseholdId == HouseholdId))
            .ToListAsync(ct);
        return rules.Select(RuleHelpers.MapToResponse).ToList();
    }
}

file static class RuleHelpers
{
    public static IQueryable<MealPlanRule> WithNav(IQueryable<MealPlanRule> q) =>
        q.Include(r => r.Tags).Include(r => r.Categories).Include(r => r.Households);

    public static MealPlanRuleResponse MapToResponse(MealPlanRule r) =>
        new()
        {
            Id = r.Id, GroupId = r.GroupId, HouseholdId = r.HouseholdId,
            Day = r.Day, EntryType = r.EntryType, QueryFilterString = r.QueryFilterString,
            Tags = r.Tags.Select(t => new MealPlanRuleTagSummary { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories.Select(c => new MealPlanRuleTagSummary { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            Households = r.Households.Select(h => h.Id).ToList(),
            CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt
        };
}
