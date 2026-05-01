using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.MealPlans;

public record CreateMealPlanCommand(Guid GroupId, Guid HouseholdId, Guid UserId, CreateMealPlanRequest Request)
    : IQuery<MealPlanResponse>
{
    public async Task<MealPlanResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = Request.Title, Text = Request.Text,
            EntryType = Request.EntryType, Date = Request.Date, RecipeId = Request.RecipeId,
            GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, GroupId, HouseholdId), ct);
        await MealPlanHelpers.LoadRecipeNav(db, plan, ct);
        return MealPlanHelpers.MapToResponse(plan);
    }
}

public record UpdateMealPlanCommand(Guid HouseholdId, Guid Id, UpdateMealPlanRequest Request) : IQuery<MealPlanResponse?>
{
    public async Task<MealPlanResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = await MealPlanHelpers.WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == HouseholdId && m.Id == Id, ct);
        if (plan is null) return null;

        if (Request.Title is not null) plan.Title = Request.Title;
        if (Request.Text is not null) plan.Text = Request.Text;
        if (Request.EntryType is not null) plan.EntryType = Request.EntryType;
        if (Request.Date.HasValue) plan.Date = Request.Date.Value;
        if (Request.RecipeId.HasValue) plan.RecipeId = Request.RecipeId;

        plan.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryUpdatedEvent(plan.Id, plan.GroupId, HouseholdId), ct);
        await MealPlanHelpers.LoadRecipeNav(db, plan, ct);
        return MealPlanHelpers.MapToResponse(plan);
    }
}

public record CreateRandomMealPlanCommand(Guid GroupId, Guid HouseholdId, Guid UserId, CreateRandomMealPlanRequest Request)
    : IQuery<MealPlanResponse?>
{
    public async Task<MealPlanResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");
        var recipeId = await MealPlanHelpers.GetRandomRecipeIdInternalAsync(db, logger, GroupId, Request.Date, Request.EntryType, ct);
        if (recipeId is null) return null;

        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = string.Empty, EntryType = Request.EntryType, Date = Request.Date,
            RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, GroupId, HouseholdId), ct);
        await MealPlanHelpers.LoadRecipeNav(db, plan, ct);
        return MealPlanHelpers.MapToResponse(plan);
    }
}

public record FillDayCommand(Guid GroupId, Guid HouseholdId, Guid UserId, FillDayRequest Request)
    : IQuery<IList<MealPlanResponse>>
{
    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");
        var plans = new List<MealPlan>();
        foreach (var entryType in Request.EntryTypes)
        {
            var recipeId = await MealPlanHelpers.GetRandomRecipeIdInternalAsync(db, logger, GroupId, Request.Date, entryType, ct);
            if (recipeId is null) continue;

            var plan = new MealPlan
            {
                Id = Guid.NewGuid(), Title = string.Empty, EntryType = entryType, Date = Request.Date,
                RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
                CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
            };
            db.MealPlans.Add(plan);
            plans.Add(plan);
        }
        await db.SaveChangesAsync(ct);
        foreach (var plan in plans)
        {
            await MealPlanHelpers.LoadRecipeNav(db, plan, ct);
            await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, plan.GroupId, plan.HouseholdId), ct);
        }
        return plans.Select(MealPlanHelpers.MapToResponse).ToList();
    }
}

public record FillWeekCommand(Guid GroupId, Guid HouseholdId, Guid UserId, FillWeekRequest Request)
    : IQuery<IList<MealPlanResponse>>
{
    private static readonly IList<string> DefaultFillEntryTypes = ["breakfast", "lunch", "side", "dinner", "side"];

    public async Task<IList<MealPlanResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("MealPlanCommands");

        var allRules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == GroupId && r.EntryType != "unset")
            .ToListAsync(ct);

        var hasRules = allRules.Count > 0;
        logger.LogDebug("FillWeek: {Rules} rules found for group {Group}", allRules.Count, GroupId);

        var plans = new List<MealPlan>();
        for (var date = Request.StartDate; date <= Request.EndDate; date = date.AddDays(1))
        {
            var dayName = MealPlanHelpers.DayOfWeekToRuleDay(date.DayOfWeek);
            IEnumerable<(string entryType, MealPlanRule? rule)> slots;

            if (hasRules)
            {
                var dayRules = allRules.Where(r => r.Day == dayName || r.Day == "unset").ToList();
                slots = dayRules.Select(r => (r.EntryType, (MealPlanRule?)r));
            }
            else
            {
                slots = DefaultFillEntryTypes.Select(t => (t, (MealPlanRule?)null));
            }

            foreach (var (entryType, rule) in slots)
            {
                Guid? recipeId = rule is { Tags.Count: > 0 } or { Categories.Count: > 0 }
                    ? await MealPlanHelpers.GetRandomRecipeIdForRuleAsync(db, logger, GroupId, rule, ct)
                    : await MealPlanHelpers.GetRandomRecipeIdInternalAsync(db, logger, GroupId, date, entryType, ct);

                if (recipeId is null) continue;

                var plan = new MealPlan
                {
                    Id = Guid.NewGuid(), Title = string.Empty, EntryType = entryType, Date = date,
                    RecipeId = recipeId, GroupId = GroupId, HouseholdId = HouseholdId, UserId = UserId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.MealPlans.Add(plan);
                plans.Add(plan);
            }
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans)
        {
            await MealPlanHelpers.LoadRecipeNav(db, plan, ct);
            await services.Mediator.Publish(new MealPlanEntryCreatedEvent(plan.Id, plan.GroupId, plan.HouseholdId), ct);
        }
        return plans.Select(MealPlanHelpers.MapToResponse).ToList();
    }
}

public record DeleteMealPlanCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var plan = await db.MealPlans.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.HouseholdId == HouseholdId && m.Id == Id, ct);
        if (plan is null) return false;
        db.MealPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new MealPlanEntryDeletedEvent(Id, HouseholdId), ct);
        return true;
    }
}

file static class MealPlanHelpers
{
    public static string DayOfWeekToRuleDay(DayOfWeek dow) => dow switch
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

    public static IQueryable<MealPlan> WithRecipe(IQueryable<MealPlan> q) =>
        q.Include(m => m.Recipe).ThenInclude(r => r!.Tags)
         .Include(m => m.Recipe).ThenInclude(r => r!.Categories);

    public static async Task LoadRecipeNav(ApplicationDbContext db, MealPlan plan, CancellationToken ct)
    {
        if (plan.RecipeId is null) return;
        await db.Entry(plan).Reference(p => p.Recipe).LoadAsync(ct);
        if (plan.Recipe is not null)
        {
            await db.Entry(plan.Recipe).Collection(r => r.Tags).LoadAsync(ct);
            await db.Entry(plan.Recipe).Collection(r => r.Categories).LoadAsync(ct);
        }
    }

    public static async Task<Guid?> GetRandomRecipeIdInternalAsync(ApplicationDbContext db, ILogger logger,
        Guid groupId, DateOnly date, string entryType, CancellationToken ct)
    {
        var dayName = DayOfWeekToRuleDay(date.DayOfWeek);
        var rules = await db.MealPlanRules.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == groupId &&
                        (r.Day == dayName || r.Day == "unset") &&
                        (r.EntryType == entryType || r.EntryType == "unset"))
            .ToListAsync(ct);

        var tagSets = rules.Where(r => r.Tags.Count > 0).Select(r => r.Tags.Select(t => t.Id).ToHashSet()).ToList();
        var catSets = rules.Where(r => r.Categories.Count > 0).Select(r => r.Categories.Select(c => c.Id).ToHashSet()).ToList();

        if (tagSets.Count > 0 || catSets.Count > 0)
        {
            var candidates = await db.Recipes.IgnoreQueryFilters()
                .Include(r => r.Tags).Include(r => r.Categories)
                .Where(r => r.GroupId == groupId).ToListAsync(ct);
            var filtered = candidates.Where(r =>
                tagSets.All(set => r.Tags.Any(t => set.Contains(t.Id))) &&
                catSets.All(set => r.Categories.Any(c => set.Contains(c.Id)))).ToList();

            logger.LogDebug("MealPlan rules filter: entryType={EntryType} rules={Rules} candidates={Candidates} filtered={Filtered}",
                entryType, rules.Count, candidates.Count, filtered.Count);

            if (filtered.Count > 0)
                return filtered[Random.Shared.Next(filtered.Count)].Id;
        }

        var entrySlug = entryType.ToLowerInvariant();
        var matchingTagIds = await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId && t.Slug == entrySlug).Select(t => t.Id).ToListAsync(ct);
        var matchingCatIds = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == groupId && c.Slug == entrySlug).Select(c => c.Id).ToListAsync(ct);

        logger.LogDebug("MealPlan auto-filter: entryType={EntryType} slug={Slug} matchingTags={Tags} matchingCats={Cats}",
            entryType, entrySlug, matchingTagIds.Count, matchingCatIds.Count);

        if (matchingTagIds.Count > 0 || matchingCatIds.Count > 0)
        {
            var autoFiltered = await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.GroupId == groupId &&
                            (r.Tags.Any(t => matchingTagIds.Contains(t.Id)) ||
                             r.Categories.Any(c => matchingCatIds.Contains(c.Id))))
                .Select(r => r.Id).ToListAsync(ct);

            logger.LogDebug("MealPlan auto-filter results: {Count} recipes match slug '{Slug}'", autoFiltered.Count, entrySlug);
            if (autoFiltered.Count > 0)
                return autoFiltered[Random.Shared.Next(autoFiltered.Count)];
        }

        logger.LogDebug("MealPlan fallback: no recipes matched slug '{Slug}', picking from all group recipes", entrySlug);
        var allIds = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId).Select(r => r.Id).ToListAsync(ct);
        return allIds.Count == 0 ? null : allIds[Random.Shared.Next(allIds.Count)];
    }

    public static async Task<Guid?> GetRandomRecipeIdForRuleAsync(ApplicationDbContext db, ILogger logger,
        Guid groupId, MealPlanRule rule, CancellationToken ct)
    {
        var tagIds = rule.Tags.Select(t => t.Id).ToHashSet();
        var catIds = rule.Categories.Select(c => c.Id).ToHashSet();
        var candidates = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Tags).Include(r => r.Categories)
            .Where(r => r.GroupId == groupId).ToListAsync(ct);
        var filtered = candidates.Where(r =>
            (tagIds.Count == 0 || r.Tags.Any(t => tagIds.Contains(t.Id))) &&
            (catIds.Count == 0 || r.Categories.Any(c => catIds.Contains(c.Id)))).ToList();

        logger.LogDebug("Rule {Rule}: {Filtered}/{Total} recipes match constraints", rule.Id, filtered.Count, candidates.Count);
        return filtered.Count == 0 ? null : filtered[Random.Shared.Next(filtered.Count)].Id;
    }

    public static MealPlanResponse MapToResponse(MealPlan m) =>
        new()
        {
            Id = m.Id, Title = m.Title, Text = m.Text, EntryType = m.EntryType, Date = m.Date,
            RecipeId = m.RecipeId,
            Recipe = m.Recipe is null ? null : new MealPlanRecipeSummary
            {
                Id = m.Recipe.Id.ToString(), Name = m.Recipe.Name, Slug = m.Recipe.Slug,
                Image = m.Recipe.Image, Description = m.Recipe.Description,
                Tags = m.Recipe.Tags.Select(t => new MealPlanRecipeTagSummary
                    { Id = t.Id.ToString(), GroupId = t.GroupId.ToString(), Name = t.Name, Slug = t.Slug }).ToList(),
                RecipeCategory = m.Recipe.Categories.Select(c => new MealPlanRecipeTagSummary
                    { Id = c.Id.ToString(), GroupId = c.GroupId.ToString(), Name = c.Name, Slug = c.Slug }).ToList()
            },
            GroupId = m.GroupId, HouseholdId = m.HouseholdId, UserId = m.UserId,
            CreatedAt = m.CreatedAt, UpdateAt = m.UpdateAt
        };
}
