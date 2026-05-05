using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.MealPlans;

public static class MealPlanRecipeSelectionService
{
    public static string DayOfWeekToRuleDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "monday",
            DayOfWeek.Tuesday => "tuesday",
            DayOfWeek.Wednesday => "wednesday",
            DayOfWeek.Thursday => "thursday",
            DayOfWeek.Friday => "friday",
            DayOfWeek.Saturday => "saturday",
            DayOfWeek.Sunday => "sunday",
            _ => "unset"
        };
    }

    public static async Task<Guid?> GetRandomRecipeIdAsync(
        ApplicationDbContext db,
        ILogger logger,
        Guid groupId,
        DateOnly date,
        string entryType,
        CancellationToken ct = default)
    {
        var dayName = DayOfWeekToRuleDay(date.DayOfWeek);
        var rules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == groupId &&
                        (r.Day == dayName || r.Day == "unset") &&
                        (r.EntryType == entryType || r.EntryType == "unset"))
            .ToListAsync(ct);

        var tagSets = rules.Where(r => r.Tags.Count > 0).Select(r => r.Tags.Select(t => t.Id).ToHashSet()).ToList();
        var categorySets = rules.Where(r => r.Categories.Count > 0)
            .Select(r => r.Categories.Select(c => c.Id).ToHashSet())
            .ToList();

        if (tagSets.Count > 0 || categorySets.Count > 0)
        {
            var candidates = await db.Recipes.IgnoreQueryFilters()
                .Include(r => r.Tags).Include(r => r.Categories)
                .Where(r => r.GroupId == groupId)
                .ToListAsync(ct);

            var filtered = candidates.Where(r =>
                tagSets.All(set => r.Tags.Any(t => set.Contains(t.Id))) &&
                categorySets.All(set => r.Categories.Any(c => set.Contains(c.Id)))).ToList();

            logger.LogDebug(
                "MealPlan rules filter: entryType={EntryType} rules={Rules} candidates={Candidates} filtered={Filtered}",
                entryType,
                rules.Count,
                candidates.Count,
                filtered.Count);

            if (filtered.Count > 0)
            {
                return filtered[Random.Shared.Next(filtered.Count)].Id;
            }
        }

        var entrySlug = entryType.ToLowerInvariant();
        var matchingTagIds = await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId && t.Slug == entrySlug)
            .Select(t => t.Id)
            .ToListAsync(ct);
        var matchingCategoryIds = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == groupId && c.Slug == entrySlug)
            .Select(c => c.Id)
            .ToListAsync(ct);

        logger.LogDebug(
            "MealPlan auto-filter: entryType={EntryType} slug={Slug} matchingTags={Tags} matchingCats={Cats}",
            entryType,
            entrySlug,
            matchingTagIds.Count,
            matchingCategoryIds.Count);

        if (matchingTagIds.Count > 0 || matchingCategoryIds.Count > 0)
        {
            var autoFiltered = await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.GroupId == groupId &&
                            (r.Tags.Any(t => matchingTagIds.Contains(t.Id)) ||
                             r.Categories.Any(c => matchingCategoryIds.Contains(c.Id))))
                .Select(r => r.Id)
                .ToListAsync(ct);

            logger.LogDebug("MealPlan auto-filter results: {Count} recipes match slug '{Slug}'", autoFiltered.Count,
                entrySlug);
            if (autoFiltered.Count > 0)
            {
                return autoFiltered[Random.Shared.Next(autoFiltered.Count)];
            }
        }

        logger.LogDebug("MealPlan fallback: no recipes matched slug '{Slug}', picking from all group recipes", entrySlug);
        var allIds = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        return allIds.Count == 0 ? null : allIds[Random.Shared.Next(allIds.Count)];
    }

    public static async Task<Guid?> GetRandomRecipeIdForRuleAsync(
        ApplicationDbContext db,
        ILogger logger,
        Guid groupId,
        MealPlanRule rule,
        CancellationToken ct = default)
    {
        var tagIds = rule.Tags.Select(t => t.Id).ToHashSet();
        var categoryIds = rule.Categories.Select(c => c.Id).ToHashSet();
        var candidates = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == groupId)
            .ToListAsync(ct);

        var filtered = candidates.Where(r =>
            (tagIds.Count == 0 || r.Tags.Any(t => tagIds.Contains(t.Id))) &&
            (categoryIds.Count == 0 || r.Categories.Any(c => categoryIds.Contains(c.Id)))).ToList();

        logger.LogDebug("Rule {Rule}: {Filtered}/{Total} recipes match constraints", rule.Id, filtered.Count,
            candidates.Count);

        return filtered.Count == 0 ? null : filtered[Random.Shared.Next(filtered.Count)].Id;
    }
}
