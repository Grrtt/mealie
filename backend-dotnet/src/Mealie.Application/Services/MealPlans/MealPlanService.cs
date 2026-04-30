using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.MealPlans;

public class MealPlanService(ApplicationDbContext db) : IMealPlanService
{
    private static IQueryable<MealPlan> WithRecipe(IQueryable<MealPlan> q) =>
        q.Include(m => m.Recipe).ThenInclude(r => r!.Tags)
         .Include(m => m.Recipe).ThenInclude(r => r!.Categories);

    public async Task<IList<MealPlanResponse>> GetMealPlansAsync(Guid householdId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default)
    {
        var query = WithRecipe(db.MealPlans.IgnoreQueryFilters()
            .Where(m => m.HouseholdId == householdId));

        if (startDate.HasValue) query = query.Where(m => m.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(m => m.Date <= endDate.Value);

        var plans = await query.OrderBy(m => m.Date).ToListAsync(ct);
        return plans.Select(MapToResponse).ToList();
    }

    public async Task<MealPlanResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var m = await WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (m is null) return null;
        return MapToResponse(m);
    }

    public async Task<MealPlanResponse> CreateAsync(Guid groupId, Guid householdId, Guid userId, CreateMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(), Title = request.Title, Text = request.Text,
            EntryType = request.EntryType, Date = request.Date,
            RecipeId = request.RecipeId, GroupId = groupId,
            HouseholdId = householdId, UserId = userId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> UpdateAsync(Guid householdId, Guid id, UpdateMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = await WithRecipe(db.MealPlans.IgnoreQueryFilters())
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (plan is null) return null;
        if (request.Title is not null) plan.Title = request.Title;
        if (request.Text is not null) plan.Text = request.Text;
        if (request.EntryType is not null) plan.EntryType = request.EntryType;
        if (request.Date.HasValue) plan.Date = request.Date.Value;
        if (request.RecipeId.HasValue) plan.RecipeId = request.RecipeId;
        plan.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> CreateRandomAsync(Guid groupId, Guid householdId, Guid userId, CreateRandomMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = await CreateRandomPlanAsync(groupId, householdId, userId, request.Date, request.EntryType, ct);
        if (plan is null) return null;
        await db.SaveChangesAsync(ct);
        await LoadRecipeNav(plan, ct);
        return MapToResponse(plan);
    }

    public async Task<IList<MealPlanResponse>> FillDayAsync(Guid groupId, Guid householdId, Guid userId, FillDayRequest request, CancellationToken ct = default)
    {
        var recipeIds = await GetGroupRecipeIdsAsync(groupId, ct);
        if (recipeIds.Count == 0) return [];

        var plans = new List<MealPlan>();
        foreach (var entryType in request.EntryTypes)
        {
            var plan = BuildRandomPlan(groupId, householdId, userId, request.Date, entryType, recipeIds);
            db.MealPlans.Add(plan);
            plans.Add(plan);
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans) await LoadRecipeNav(plan, ct);
        return plans.Select(MapToResponse).ToList();
    }

    public async Task<IList<MealPlanResponse>> FillWeekAsync(Guid groupId, Guid householdId, Guid userId, FillWeekRequest request, CancellationToken ct = default)
    {
        var recipeIds = await GetGroupRecipeIdsAsync(groupId, ct);
        if (recipeIds.Count == 0) return [];

        var plans = new List<MealPlan>();
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            foreach (var entryType in request.EntryTypes)
            {
                var plan = BuildRandomPlan(groupId, householdId, userId, date, entryType, recipeIds);
                db.MealPlans.Add(plan);
                plans.Add(plan);
            }
        }

        await db.SaveChangesAsync(ct);
        foreach (var plan in plans) await LoadRecipeNav(plan, ct);
        return plans.Select(MapToResponse).ToList();
    }

    private async Task<List<Guid>> GetGroupRecipeIdsAsync(Guid groupId, CancellationToken ct) =>
        await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId)
            .Select(r => r.Id)
            .ToListAsync(ct);

    private static MealPlan BuildRandomPlan(Guid groupId, Guid householdId, Guid userId, DateOnly date, string entryType, List<Guid> recipeIds) => new()
    {
        Id = Guid.NewGuid(),
        Title = string.Empty,
        EntryType = entryType,
        Date = date,
        RecipeId = recipeIds[Random.Shared.Next(recipeIds.Count)],
        GroupId = groupId,
        HouseholdId = householdId,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdateAt = DateTime.UtcNow,
    };

    private async Task<MealPlan?> CreateRandomPlanAsync(Guid groupId, Guid householdId, Guid userId, DateOnly date, string entryType, CancellationToken ct)
    {
        var recipeIds = await GetGroupRecipeIdsAsync(groupId, ct);
        if (recipeIds.Count == 0) return null;
        var plan = BuildRandomPlan(groupId, householdId, userId, date, entryType, recipeIds);
        db.MealPlans.Add(plan);
        return plan;
    }

    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var plan = await db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (plan is null) return false;
        db.MealPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task LoadRecipeNav(MealPlan plan, CancellationToken ct)
    {
        if (plan.RecipeId is null) return;
        await db.Entry(plan).Reference(p => p.Recipe).LoadAsync(ct);
        if (plan.Recipe is not null)
        {
            await db.Entry(plan.Recipe).Collection(r => r.Tags).LoadAsync(ct);
            await db.Entry(plan.Recipe).Collection(r => r.Categories).LoadAsync(ct);
        }
    }

    private static MealPlanResponse MapToResponse(MealPlan m) => new()
    {
        Id = m.Id,
        Title = m.Title,
        Text = m.Text,
        EntryType = m.EntryType,
        Date = m.Date,
        RecipeId = m.RecipeId,
        Recipe = m.Recipe is null ? null : new MealPlanRecipeSummary
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
                Slug = t.Slug,
            }).ToList(),
            RecipeCategory = m.Recipe.Categories.Select(c => new MealPlanRecipeTagSummary
            {
                Id = c.Id.ToString(),
                GroupId = c.GroupId.ToString(),
                Name = c.Name,
                Slug = c.Slug,
            }).ToList(),
        },
        GroupId = m.GroupId,
        HouseholdId = m.HouseholdId,
        UserId = m.UserId,
        CreatedAt = m.CreatedAt,
        UpdateAt = m.UpdateAt,
    };
}
