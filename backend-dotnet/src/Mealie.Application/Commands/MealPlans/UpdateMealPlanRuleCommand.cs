using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record UpdateMealPlanRuleCommand(Guid GroupId, Guid Id, UpdateMealPlanRuleRequest Request)
    : IQuery<MealPlanRuleResponse?>
{
    public async Task<MealPlanRuleResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var rule = await RuleHelpers.WithNav(db.MealPlanRules.IgnoreQueryFilters())
            .FirstOrDefaultAsync(r => r.GroupId == GroupId && r.Id == Id, ct);
        if (rule is null) return null;

        if (Request.Day is not null) rule.Day = Request.Day;
        if (Request.EntryType is not null) rule.EntryType = Request.EntryType;
        if (Request.QueryFilterString is not null) rule.QueryFilterString = Request.QueryFilterString;
        rule.UpdateAt = DateTime.UtcNow;

        if (Request.TagIds is not null)
        {
            rule.Tags.Clear();
            var tags = await db.Tags.IgnoreQueryFilters().Where(t => Request.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags) rule.Tags.Add(tag);
        }
        if (Request.CategoryIds is not null)
        {
            rule.Categories.Clear();
            var cats = await db.Categories.IgnoreQueryFilters().Where(c => Request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in cats) rule.Categories.Add(cat);
        }
        if (Request.HouseholdIds is not null)
        {
            rule.Households.Clear();
            var households = await db.Households.Where(h => Request.HouseholdIds.Contains(h.Id)).ToListAsync(ct);
            foreach (var h in households) rule.Households.Add(h);
        }

        await db.SaveChangesAsync(ct);
        return RuleHelpers.MapToResponse(rule);
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