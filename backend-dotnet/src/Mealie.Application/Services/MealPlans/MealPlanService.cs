using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.MealPlans;

public class MealPlanService(ApplicationDbContext db, IMediator mediator, ILogger<MealPlanService> logger)
    : IMealPlanService
{
    private static readonly IList<string> DefaultFillEntryTypes = ["breakfast", "lunch", "side", "dinner", "side"];

    public async Task<IList<MealPlanResponse>> GetMealPlansAsync(Guid householdId, DateOnly? startDate = null,
        DateOnly? endDate = null, CancellationToken ct = default)
    {
        var query = WithRecipe(db.MealPlans.IgnoreQueryFilters()
            .Where(m => m.HouseholdId == householdId));

        if (startDate.HasValue)
        {
            query = query.Where(m => m.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Date <= endDate.Value);
        }

        var plans = await query.OrderBy(m => m.Date).ToListAsync(ct);
        return plans.Select(MapToResponse).ToList();
    }

    public async Task<IList<MealPlanResponse>> GetTodayAsync(Guid householdId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plans = await WithRecipe(db.MealPlans.IgnoreQueryFilters()
            .Where(m => m.HouseholdId == householdId && m.Date == today))
            .ToListAsync(ct);
        return plans.Select(MapToResponse).ToList();
    }

    public async Task<Guid?> GetRandomRecipeIdAsync(Guid groupId, DateOnly date, string entryType,
        CancellationToken ct = default)
    {
        return await GetRandomRecipeIdInternalAsync(groupId, date, entryType, ct);
    }

    public async Task<MealPlanResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var m = await WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        return m is null ? null : MapToResponse(m);
    }

    public async Task<MealPlanResponse> CreateAsync(Guid groupId, Guid householdId, Guid userId,
        CreateMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = request.Title, Text = request.Text,
            EntryType = request.EntryType, Date = request.Date,
            RecipeId = request.RecipeId, GroupId = groupId,
            HouseholdId = householdId, UserId = userId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, groupId, householdId), ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> UpdateAsync(Guid householdId, Guid id, UpdateMealPlanRequest request,
        CancellationToken ct = default)
    {
        var plan = await WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (plan is null)
        {
            return null;
        }

        if (request.Title is not null)
        {
            plan.Title = request.Title;
        }

        if (request.Text is not null)
        {
            plan.Text = request.Text;
        }

        if (request.EntryType is not null)
        {
            plan.EntryType = request.EntryType;
        }

        if (request.Date.HasValue)
        {
            plan.Date = request.Date.Value;
        }

        if (request.RecipeId.HasValue)
        {
            plan.RecipeId = request.RecipeId;
        }

        plan.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new MealPlanEntryUpdatedEvent(plan.Id, plan.GroupId, householdId), ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> CreateRandomAsync(Guid groupId, Guid householdId, Guid userId,
        CreateRandomMealPlanRequest request, CancellationToken ct = default)
    {
        var recipeId = await GetRandomRecipeIdInternalAsync(groupId, request.Date, request.EntryType, ct);
        if (recipeId is null)
        {
            return null;
        }

        var plan = new MealPlan
        {
            Id = Guid.NewGuid(),
            Title = string.Empty,
            EntryType = request.EntryType,
            Date = request.Date,
            RecipeId = recipeId,
            GroupId = groupId,
            HouseholdId = householdId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, groupId, householdId), ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<IList<MealPlanResponse>> FillDayAsync(Guid groupId, Guid householdId, Guid userId,
        FillDayRequest request, CancellationToken ct = default)
    {
        var plans = new List<MealPlan>();
        foreach (var entryType in request.EntryTypes)
        {
            var recipeId = await GetRandomRecipeIdInternalAsync(groupId, request.Date, entryType, ct);
            if (recipeId is null)
            {
                continue;
            }

            var plan = new MealPlan
            {
                Id = Guid.NewGuid(),
                Title = string.Empty,
                EntryType = entryType,
                Date = request.Date,
                RecipeId = recipeId,
                GroupId = groupId,
                HouseholdId = householdId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };
            db.MealPlans.Add(plan);
            plans.Add(plan);
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans)
        {
            await LoadRecipeNav(plan, ct);
            await mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, plan.GroupId, plan.HouseholdId), ct);
        }

        return plans.Select(MapToResponse).ToList();
    }

    public async Task<IList<MealPlanResponse>> FillWeekAsync(Guid groupId, Guid householdId, Guid userId,
        FillWeekRequest request, CancellationToken ct = default)
    {
        // Load all rules for this group that have an explicit entry type — these drive what gets created.
        var allRules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Where(r => r.GroupId == groupId && r.EntryType != "unset")
            .ToListAsync(ct);

        var hasRules = allRules.Count > 0;
        logger.LogDebug("FillWeek: {Rules} rules found for group {Group}", allRules.Count, groupId);

        var plans = new List<MealPlan>();
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            var dayName = DayOfWeekToRuleDay(date.DayOfWeek);

            IEnumerable<(string entryType, MealPlanRule? rule)> slots;

            if (hasRules)
            {
                // Use rules for this specific day (or "unset" rules that apply to every day)
                var dayRules = allRules
                    .Where(r => r.Day == dayName || r.Day == "unset")
                    .ToList();

                slots = dayRules.Select(r => (r.EntryType, (MealPlanRule?)r));
            }
            else
            {
                // No rules configured — fall back to the standard set of entry types
                slots = DefaultFillEntryTypes.Select(t => (t, (MealPlanRule?)null));
            }

            foreach (var (entryType, rule) in slots)
            {
                Guid? recipeId;

                if (rule is { Tags.Count: > 0 } or { Categories.Count: > 0 })
                {
                    // Rule has explicit tag/category constraints — apply them directly
                    recipeId = await GetRandomRecipeIdForRuleAsync(groupId, rule, ct);
                }
                else
                {
                    recipeId = await GetRandomRecipeIdInternalAsync(groupId, date, entryType, ct);
                }

                if (recipeId is null)
                {
                    continue;
                }

                var plan = new MealPlan
                {
                    Id = Guid.NewGuid(),
                    Title = string.Empty,
                    EntryType = entryType,
                    Date = date,
                    RecipeId = recipeId,
                    GroupId = groupId,
                    HouseholdId = householdId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };
                db.MealPlans.Add(plan);
                plans.Add(plan);
            }
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans)
        {
            await LoadRecipeNav(plan, ct);
            await mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, plan.GroupId, plan.HouseholdId), ct);
        }

        return plans.Select(MapToResponse).ToList();
    }


    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var plan = await db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (plan is null)
        {
            return false;
        }

        db.MealPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new MealPlanEntryDeletedEvent(id, householdId), ct);
        return true;
    }

    private static string DayOfWeekToRuleDay(DayOfWeek dow)
    {
        return dow switch
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

    /// <summary>
    ///     Returns a random recipe ID from the group, filtered by applicable meal plan rules for the
    ///     given date and entry type. When no rules with tag/category constraints are configured,
    ///     automatically filters to recipes whose tags or categories match the entry type by slug.
    ///     Falls back to all group recipes only if nothing matches.
    /// </summary>
    private async Task<Guid?> GetRandomRecipeIdInternalAsync(Guid groupId, DateOnly date, string entryType,
        CancellationToken ct)
    {
        var dayName = DayOfWeekToRuleDay(date.DayOfWeek);

        // Load rules that match (day OR unset) AND (entryType OR unset)
        var rules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Where(r => r.GroupId == groupId &&
                        (r.Day == dayName || r.Day == "unset") &&
                        (r.EntryType == entryType || r.EntryType == "unset"))
            .ToListAsync(ct);

        var tagSets = rules.Where(r => r.Tags.Count > 0).Select(r => r.Tags.Select(t => t.Id).ToHashSet()).ToList();
        var catSets = rules.Where(r => r.Categories.Count > 0).Select(r => r.Categories.Select(c => c.Id).ToHashSet())
            .ToList();

        if (tagSets.Count > 0 || catSets.Count > 0)
        {
            var candidates = await db.Recipes.IgnoreQueryFilters()
                .Include(r => r.Tags)
                .Include(r => r.Categories)
                .Where(r => r.GroupId == groupId)
                .ToListAsync(ct);

            var filtered = candidates.Where(r =>
                tagSets.All(set => r.Tags.Any(t => set.Contains(t.Id))) &&
                catSets.All(set => r.Categories.Any(c => set.Contains(c.Id)))
            ).ToList();

            logger.LogDebug(
                "MealPlan rules filter: entryType={EntryType} rules={Rules} candidates={Candidates} filtered={Filtered}",
                entryType, rules.Count, candidates.Count, filtered.Count);

            if (filtered.Count > 0)
            {
                return filtered[Random.Shared.Next(filtered.Count)].Id;
            }
        }

        // Auto-filter: query tag and category IDs with explicit IgnoreQueryFilters so the
        // global tenant filter on Tag/Category doesn't interfere, then filter recipes by those IDs.
        var entrySlug = entryType.ToLowerInvariant();

        var matchingTagIds = await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId && t.Slug == entrySlug)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var matchingCatIds = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == groupId && c.Slug == entrySlug)
            .Select(c => c.Id)
            .ToListAsync(ct);

        logger.LogDebug(
            "MealPlan auto-filter: entryType={EntryType} slug={Slug} matchingTags={Tags} matchingCats={Cats}",
            entryType, entrySlug, matchingTagIds.Count, matchingCatIds.Count);

        if (matchingTagIds.Count > 0 || matchingCatIds.Count > 0)
        {
            var autoFiltered = await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.GroupId == groupId &&
                            (r.Tags.Any(t => matchingTagIds.Contains(t.Id)) ||
                             r.Categories.Any(c => matchingCatIds.Contains(c.Id))))
                .Select(r => r.Id)
                .ToListAsync(ct);

            logger.LogDebug("MealPlan auto-filter results: {Count} recipes match slug '{Slug}'", autoFiltered.Count,
                entrySlug);

            if (autoFiltered.Count > 0)
            {
                return autoFiltered[Random.Shared.Next(autoFiltered.Count)];
            }
        }

        // Total fallback — no tag/category matches found for this entry type
        logger.LogDebug("MealPlan fallback: no recipes matched slug '{Slug}', picking from all group recipes",
            entrySlug);

        var allIds = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        return allIds.Count == 0 ? null : allIds[Random.Shared.Next(allIds.Count)];
    }

    private static IQueryable<MealPlan> WithRecipe(IQueryable<MealPlan> q)
    {
        return q.Include(m => m.Recipe).ThenInclude(r => r!.Tags)
            .Include(m => m.Recipe).ThenInclude(r => r!.Categories);
    }

    /// <summary>Picks a random recipe that satisfies all tag and category constraints of the given rule.</summary>
    private async Task<Guid?> GetRandomRecipeIdForRuleAsync(Guid groupId, MealPlanRule rule, CancellationToken ct)
    {
        var tagIds = rule.Tags.Select(t => t.Id).ToHashSet();
        var catIds = rule.Categories.Select(c => c.Id).ToHashSet();

        var candidates = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Where(r => r.GroupId == groupId)
            .ToListAsync(ct);

        var filtered = candidates.Where(r =>
            (tagIds.Count == 0 || r.Tags.Any(t => tagIds.Contains(t.Id))) &&
            (catIds.Count == 0 || r.Categories.Any(c => catIds.Contains(c.Id)))
        ).ToList();

        logger.LogDebug("Rule {Rule}: {Filtered}/{Total} recipes match constraints", rule.Id, filtered.Count,
            candidates.Count);

        return filtered.Count == 0 ? null : filtered[Random.Shared.Next(filtered.Count)].Id;
    }

    private async Task LoadRecipeNav(MealPlan plan, CancellationToken ct)
    {
        if (plan.RecipeId is null)
        {
            return;
        }

        await db.Entry(plan).Reference(p => p.Recipe).LoadAsync(ct);
        if (plan.Recipe is not null)
        {
            await db.Entry(plan.Recipe).Collection(r => r.Tags).LoadAsync(ct);
            await db.Entry(plan.Recipe).Collection(r => r.Categories).LoadAsync(ct);
        }
    }

    private static MealPlanResponse MapToResponse(MealPlan m)
    {
        return new MealPlanResponse
        {
            Id = m.Id,
            Title = m.Title,
            Text = m.Text,
            EntryType = m.EntryType,
            Date = m.Date,
            RecipeId = m.RecipeId,
            Recipe = m.Recipe is null
                ? null
                : new MealPlanRecipeSummary
                {
                    Id = m.Recipe.Id.ToString(),
                    Name = m.Recipe.Name,
                    Slug = m.Recipe.Slug,
                    Image = m.Recipe.Image,
                    Description = m.Recipe.Description,
                    Tags = m.Recipe.Tags.Select(t => new MealPlanRecipeTagSummary
                    {
                        Id = t.Id.ToString(),
                        GroupId = t.GroupId.ToString(),
                        Name = t.Name,
                        Slug = t.Slug
                    }).ToList(),
                    RecipeCategory = m.Recipe.Categories.Select(c => new MealPlanRecipeTagSummary
                    {
                        Id = c.Id.ToString(),
                        GroupId = c.GroupId.ToString(),
                        Name = c.Name,
                        Slug = c.Slug
                    }).ToList()
                },
            GroupId = m.GroupId,
            HouseholdId = m.HouseholdId,
            UserId = m.UserId,
            CreatedAt = m.CreatedAt,
            UpdateAt = m.UpdateAt
        };
    }
}
