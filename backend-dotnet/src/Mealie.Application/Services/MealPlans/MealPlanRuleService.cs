using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.MealPlans;

public class MealPlanRuleService(ApplicationDbContext db) : IMealPlanRuleService
{
    public async Task<IList<MealPlanRuleResponse>> GetAllAsync(Guid groupId, Guid householdId,
        CancellationToken ct = default)
    {
        var rules = await WithNav(db.MealPlanRules.IgnoreQueryFilters())
            .Where(r => r.GroupId == groupId &&
                        (r.HouseholdId == null || r.HouseholdId == householdId))
            .ToListAsync(ct);
        return rules.Select(MapToResponse).ToList();
    }

    public async Task<MealPlanRuleResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var rule = await WithNav(db.MealPlanRules.IgnoreQueryFilters())
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.Id == id, ct);
        return rule is null ? null : MapToResponse(rule);
    }

    public async Task<MealPlanRuleResponse> CreateAsync(Guid groupId, Guid householdId,
        CreateMealPlanRuleRequest request, CancellationToken ct = default)
    {
        var rule = new MealPlanRule
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            HouseholdId = householdId,
            Day = request.Day,
            EntryType = request.EntryType,
            QueryFilterString = request.QueryFilterString,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await AttachNavigations(rule, request.TagIds, request.CategoryIds, request.HouseholdIds, ct);
        db.MealPlanRules.Add(rule);
        await db.SaveChangesAsync(ct);
        await LoadNav(rule, ct);
        return MapToResponse(rule);
    }

    public async Task<MealPlanRuleResponse?> UpdateAsync(Guid groupId, Guid id, UpdateMealPlanRuleRequest request,
        CancellationToken ct = default)
    {
        var rule = await WithNav(db.MealPlanRules.IgnoreQueryFilters())
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.Id == id, ct);
        if (rule is null)
        {
            return null;
        }

        if (request.Day is not null)
        {
            rule.Day = request.Day;
        }

        if (request.EntryType is not null)
        {
            rule.EntryType = request.EntryType;
        }

        if (request.QueryFilterString is not null)
        {
            rule.QueryFilterString = request.QueryFilterString;
        }

        rule.UpdateAt = DateTime.UtcNow;

        if (request.TagIds is not null)
        {
            rule.Tags.Clear();
            var tags = await db.Tags.IgnoreQueryFilters().Where(t => request.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags)
            {
                rule.Tags.Add(tag);
            }
        }

        if (request.CategoryIds is not null)
        {
            rule.Categories.Clear();
            var cats = await db.Categories.IgnoreQueryFilters().Where(c => request.CategoryIds.Contains(c.Id))
                .ToListAsync(ct);
            foreach (var cat in cats)
            {
                rule.Categories.Add(cat);
            }
        }

        if (request.HouseholdIds is not null)
        {
            rule.Households.Clear();
            var households = await db.Households.Where(h => request.HouseholdIds.Contains(h.Id)).ToListAsync(ct);
            foreach (var h in households)
            {
                rule.Households.Add(h);
            }
        }

        await db.SaveChangesAsync(ct);
        return MapToResponse(rule);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var rule = await db.MealPlanRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.Id == id, ct);
        if (rule is null)
        {
            return false;
        }

        db.MealPlanRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<MealPlanRule> WithNav(IQueryable<MealPlanRule> q)
    {
        return q.Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Households);
    }

    private async Task AttachNavigations(MealPlanRule rule, IList<Guid> tagIds, IList<Guid> categoryIds,
        IList<Guid> householdIds, CancellationToken ct)
    {
        if (tagIds.Count > 0)
        {
            var tags = await db.Tags.IgnoreQueryFilters().Where(t => tagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags)
            {
                rule.Tags.Add(tag);
            }
        }

        if (categoryIds.Count > 0)
        {
            var cats = await db.Categories.IgnoreQueryFilters().Where(c => categoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in cats)
            {
                rule.Categories.Add(cat);
            }
        }

        if (householdIds.Count > 0)
        {
            var households = await db.Households.Where(h => householdIds.Contains(h.Id)).ToListAsync(ct);
            foreach (var h in households)
            {
                rule.Households.Add(h);
            }
        }
    }

    private async Task LoadNav(MealPlanRule rule, CancellationToken ct)
    {
        await db.Entry(rule).Collection(r => r.Tags).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Categories).LoadAsync(ct);
        await db.Entry(rule).Collection(r => r.Households).LoadAsync(ct);
    }

    private static MealPlanRuleResponse MapToResponse(MealPlanRule r)
    {
        return new MealPlanRuleResponse
        {
            Id = r.Id,
            GroupId = r.GroupId,
            HouseholdId = r.HouseholdId,
            Day = r.Day,
            EntryType = r.EntryType,
            QueryFilterString = r.QueryFilterString,
            Tags = r.Tags.Select(t => new MealPlanRuleTagSummary { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories
                .Select(c => new MealPlanRuleTagSummary { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            Households = r.Households.Select(h => h.Id).ToList(),
            CreatedAt = r.CreatedAt,
            UpdateAt = r.UpdateAt
        };
    }
}
