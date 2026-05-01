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

public record GetMealPlanRuleByIdQuery(Guid GroupId, Guid Id) : IQuery<MealPlanRuleResponse?>
{
    public async Task<MealPlanRuleResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var rule = await RuleHelpers.WithNav(services.Db.MealPlanRules.IgnoreQueryFilters())
            .FirstOrDefaultAsync(r => r.GroupId == GroupId && r.Id == Id, ct);
        return rule is null ? null : RuleHelpers.MapToResponse(rule);
    }
}

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
            foreach (var tag in tags) rule.Tags.Add(tag);
        }
        if (Request.CategoryIds.Count > 0)
        {
            var cats = await db.Categories.IgnoreQueryFilters().Where(c => Request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in cats) rule.Categories.Add(cat);
        }
        if (Request.HouseholdIds.Count > 0)
        {
            var households = await db.Households.Where(h => Request.HouseholdIds.Contains(h.Id)).ToListAsync(ct);
            foreach (var h in households) rule.Households.Add(h);
        }

        db.MealPlanRules.Add(rule);
        await db.SaveChangesAsync(ct);

        await db.Entry(rule).Collection(r => r.Tags).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Categories).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Households).LoadAsync(ct);
        return RuleHelpers.MapToResponse(rule);
    }
}

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
