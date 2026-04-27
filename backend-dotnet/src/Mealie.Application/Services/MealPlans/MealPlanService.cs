using Mealie.Application.Dtos.MealPlans;
using Mealie.Domain.Entities.Planning;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.MealPlans;

public class MealPlanService(ApplicationDbContext db) : IMealPlanService
{
    public async Task<IList<MealPlanResponse>> GetMealPlansAsync(Guid householdId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default)
    {
        var query = db.MealPlans.IgnoreQueryFilters()
            .Where(m => m.HouseholdId == householdId)
            .Include(m => m.Recipe)
            .AsQueryable();

        if (startDate.HasValue) query = query.Where(m => m.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(m => m.Date <= endDate.Value);

        return await query.OrderBy(m => m.Date)
            .Select(m => MapToResponse(m))
            .ToListAsync(ct);
    }

    public async Task<MealPlanResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var m = await db.MealPlans.IgnoreQueryFilters()
            .Include(m => m.Recipe)
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
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> UpdateAsync(Guid householdId, Guid id, UpdateMealPlanRequest request, CancellationToken ct = default)
    {
        var plan = await db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Id == id, ct);
        if (plan is null) return null;
        if (request.Title is not null) plan.Title = request.Title;
        if (request.Text is not null) plan.Text = request.Text;
        if (request.EntryType is not null) plan.EntryType = request.EntryType;
        if (request.Date.HasValue) plan.Date = request.Date.Value;
        if (request.RecipeId.HasValue) plan.RecipeId = request.RecipeId;
        plan.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(plan);
    }

    public async Task<MealPlanResponse?> CreateRandomAsync(Guid groupId, Guid householdId, Guid userId, CreateRandomMealPlanRequest request, CancellationToken ct = default)
    {
        var recipeIds = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (recipeIds.Count == 0) return null;

        var randomId = recipeIds[Random.Shared.Next(recipeIds.Count)];

        var plan = new MealPlan
        {
            Id = Guid.NewGuid(),
            Title = string.Empty,
            EntryType = request.EntryType,
            Date = request.Date,
            RecipeId = randomId,
            GroupId = groupId,
            HouseholdId = householdId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };
        db.MealPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        await db.Entry(plan).Reference(p => p.Recipe).LoadAsync(ct);
        return MapToResponse(plan);
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

    private static MealPlanResponse MapToResponse(MealPlan m) => new()
    {
        Id = m.Id, Title = m.Title, Text = m.Text, EntryType = m.EntryType, Date = m.Date,
        RecipeId = m.RecipeId, RecipeSlug = m.Recipe?.Slug, RecipeName = m.Recipe?.Name,
        GroupId = m.GroupId, HouseholdId = m.HouseholdId, UserId = m.UserId,
        CreatedAt = m.CreatedAt, UpdateAt = m.UpdateAt,
    };
}
