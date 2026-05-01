using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.MealPlans;

public record CreateMealPlanRuleCommand(Guid GroupId, Guid HouseholdId, CreateMealPlanRuleRequest Request)
    : IQuery<MealPlanRuleResponse>
{
    public async Task<MealPlanRuleResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var rule = new MealPlanRule
        {
            Id = Guid.NewGuid(), GroupId = GroupId, HouseholdId = HouseholdId,
            Day = Request.Day, EntryType = Request.EntryType,
            QueryFilterString = Request.QueryFilterString,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };

        if (Request.TagIds.Count > 0)
        {
            var tags = await db.Tags.IgnoreQueryFilters().Where(t => Request.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags)
            {
                rule.Tags.Add(tag);
            }
        }

        if (Request.CategoryIds.Count > 0)
        {
            var cats = await db.Categories.IgnoreQueryFilters().Where(c => Request.CategoryIds.Contains(c.Id))
                .ToListAsync(ct);
            foreach (var cat in cats)
            {
                rule.Categories.Add(cat);
            }
        }

        if (Request.HouseholdIds.Count > 0)
        {
            var households = await db.Households.Where(h => Request.HouseholdIds.Contains(h.Id)).ToListAsync(ct);
            foreach (var h in households)
            {
                rule.Households.Add(h);
            }
        }

        db.MealPlanRules.Add(rule);
        await db.SaveChangesAsync(ct);

        await db.Entry(rule).Collection(r => r.Tags).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Categories).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Households).LoadAsync(ct);
        return RuleHelpers.MapToResponse(rule);
    }
}

file static class RuleHelpers
{
    public static IQueryable<MealPlanRule> WithNav(IQueryable<MealPlanRule> q)
    {
        return q.Include(r => r.Tags).Include(r => r.Categories).Include(r => r.Households);
    }

    public static MealPlanRuleResponse MapToResponse(MealPlanRule r)
    {
        return new MealPlanRuleResponse
        {
            Id = r.Id, GroupId = r.GroupId, HouseholdId = r.HouseholdId,
            Day = r.Day, EntryType = r.EntryType, QueryFilterString = r.QueryFilterString,
            Tags = r.Tags.Select(t => new MealPlanRuleTagSummary { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories
                .Select(c => new MealPlanRuleTagSummary { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            Households = r.Households.Select(h => h.Id).ToList(),
            CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt
        };
    }
}
