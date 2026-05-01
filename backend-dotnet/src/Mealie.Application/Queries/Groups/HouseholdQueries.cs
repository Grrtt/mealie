using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetHouseholdQuery(Guid HouseholdId) : IQuery<HouseholdResponse?>
{
    public async Task<HouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var h = await services.Db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        return h is null ? null : HouseholdMappings.MapToResponse(h);
    }
}

public record GetHouseholdsForGroupQuery(Guid GroupId) : IQuery<IList<HouseholdResponse>>
{
    public async Task<IList<HouseholdResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == GroupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }
}

public record GetHouseholdMembersQuery(Guid HouseholdId) : IQuery<IList<UserSummaryDto>>
{
    public async Task<IList<UserSummaryDto>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Users.IgnoreQueryFilters()
            .Where(u => u.HouseholdId == HouseholdId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }
}

public record GetHouseholdStatisticsQuery(Guid HouseholdId) : IQuery<HouseholdStatisticsResponse>
{
    public async Task<HouseholdStatisticsResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var totalRecipes = await db.Recipes.IgnoreQueryFilters().CountAsync(r => r.HouseholdId == HouseholdId, ct);
        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(u => u.HouseholdId == HouseholdId, ct);
        var household = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);

        int totalCategories = 0, totalTags = 0, totalTools = 0;
        if (household is not null)
        {
            totalCategories = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.GroupId == household.GroupId, ct);
            totalTags = await db.Tags.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
            totalTools = await db.Tools.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
        }

        return new HouseholdStatisticsResponse
        {
            TotalRecipes = totalRecipes, TotalUsers = totalUsers,
            TotalCategories = totalCategories, TotalTags = totalTags, TotalTools = totalTools
        };
    }
}

public record GetHouseholdPreferencesQuery(Guid HouseholdId) : IQuery<HouseholdPreferencesResponse?>
{
    public async Task<HouseholdPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var prefs = await services.Db.HouseholdPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.HouseholdId == HouseholdId, ct);
        return prefs is null ? null : HouseholdMappings.MapToHouseholdPreferencesResponse(prefs);
    }
}

file static class HouseholdMappings
{
    public static HouseholdResponse MapToResponse(Household h) =>
        new() { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId };

    public static HouseholdPreferencesResponse MapToHouseholdPreferencesResponse(HouseholdPreferences prefs) =>
        new()
        {
            Id = prefs.Id, HouseholdId = prefs.HouseholdId, PrivateHousehold = prefs.PrivateHousehold,
            FirstDayOfWeek = prefs.FirstDayOfWeek, RecipePublic = prefs.RecipePublic,
            RecipeShowNutrition = prefs.RecipeShowNutrition, RecipeShowAssets = prefs.RecipeShowAssets,
            RecipeLandscapeView = prefs.RecipeLandscapeView, RecipeDisableComments = prefs.RecipeDisableComments,
            RecipeDisableAmount = prefs.RecipeDisableAmount
        };
}
