using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Households;

public class HouseholdService(ApplicationDbContext db, ILogger<HouseholdService> logger) : IHouseholdService
{
    public async Task<HouseholdResponse?> GetHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return null;
        return MapToResponse(h);
    }

    public async Task<IList<HouseholdResponse>> GetHouseholdsForGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == groupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }

    public async Task<HouseholdResponse?> UpdateHouseholdAsync(Guid householdId, UpdateHouseholdRequest request, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return null;
        h.Name = request.Name;
        h.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(h);
    }

    public async Task<HouseholdResponse> CreateHouseholdAsync(Guid groupId, CreateHouseholdRequest request, CancellationToken ct = default)
    {
        var slug = SlugHelper.Generate(request.Name);
        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return MapToResponse(household);
    }

    public async Task<bool> DeleteHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return false;
        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<UserSummaryDto>> GetMembersAsync(Guid householdId, CancellationToken ct = default)
    {
        return await db.Users.IgnoreQueryFilters()
            .Where(u => u.HouseholdId == householdId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }

    public async Task<HouseholdStatisticsResponse> GetStatisticsAsync(Guid householdId, CancellationToken ct = default)
    {
        var totalRecipes = await db.Recipes.IgnoreQueryFilters().CountAsync(r => r.HouseholdId == householdId, ct);
        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(u => u.HouseholdId == householdId, ct);

        var household = await db.Households.IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == householdId, ct);

        int totalCategories = 0, totalTags = 0, totalTools = 0;
        if (household is not null)
        {
            totalCategories = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.GroupId == household.GroupId, ct);
            totalTags = await db.Tags.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
            totalTools = await db.Tools.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
        }

        return new HouseholdStatisticsResponse
        {
            TotalRecipes = totalRecipes,
            TotalUsers = totalUsers,
            TotalCategories = totalCategories,
            TotalTags = totalTags,
            TotalTools = totalTools
        };
    }

    public async Task<HouseholdPreferencesResponse?> GetHouseholdPreferencesAsync(Guid householdId, CancellationToken ct = default)
    {
        var prefs = await db.HouseholdPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.HouseholdId == householdId, ct);
        if (prefs is null) return null;
        return MapToHouseholdPreferencesResponse(prefs);
    }

    public async Task<HouseholdPreferencesResponse?> UpdateHouseholdPreferencesAsync(Guid householdId, UpdateHouseholdPreferencesRequest request, CancellationToken ct = default)
    {
        var prefs = await db.HouseholdPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.HouseholdId == householdId, ct);
        if (prefs is null) return null;

        if (request.PrivateHousehold.HasValue)
            prefs.PrivateHousehold = request.PrivateHousehold.Value;
        if (request.FirstDayOfWeek is not null)
            prefs.FirstDayOfWeek = request.FirstDayOfWeek;
        if (request.RecipePublic is not null)
            prefs.RecipePublic = request.RecipePublic;
        if (request.RecipeShowNutrition is not null)
            prefs.RecipeShowNutrition = request.RecipeShowNutrition;
        if (request.RecipeShowAssets is not null)
            prefs.RecipeShowAssets = request.RecipeShowAssets;
        if (request.RecipeLandscapeView is not null)
            prefs.RecipeLandscapeView = request.RecipeLandscapeView;
        if (request.RecipeDisableComments is not null)
            prefs.RecipeDisableComments = request.RecipeDisableComments;
        if (request.RecipeDisableAmount is not null)
            prefs.RecipeDisableAmount = request.RecipeDisableAmount;

        prefs.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToHouseholdPreferencesResponse(prefs);
    }

    private static HouseholdResponse MapToResponse(Household h) => new()
    {
        Id = h.Id,
        Name = h.Name,
        Slug = h.Slug,
        GroupId = h.GroupId
    };

    private static HouseholdPreferencesResponse MapToHouseholdPreferencesResponse(HouseholdPreferences prefs) => new()
    {
        Id = prefs.Id,
        HouseholdId = prefs.HouseholdId,
        PrivateHousehold = prefs.PrivateHousehold,
        FirstDayOfWeek = prefs.FirstDayOfWeek,
        RecipePublic = prefs.RecipePublic,
        RecipeShowNutrition = prefs.RecipeShowNutrition,
        RecipeShowAssets = prefs.RecipeShowAssets,
        RecipeLandscapeView = prefs.RecipeLandscapeView,
        RecipeDisableComments = prefs.RecipeDisableComments,
        RecipeDisableAmount = prefs.RecipeDisableAmount,
    };

    public async Task<InviteTokenResponse> CreateHouseholdInviteTokenAsync(Guid groupId, Guid householdId, CreateInviteTokenRequest request, CancellationToken ct = default)
    {
        var token = new GroupInviteToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.InviteTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return new InviteTokenResponse { Id = token.Id, Token = token.Token, GroupId = groupId, HouseholdId = householdId };
    }

    public async Task<bool> UpdateMemberPermissionsAsync(Guid householdId, Guid userId, bool admin, bool canOrganize, bool canInvite, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.HouseholdId == householdId, ct);
        if (user is null) return false;

        user.Admin = admin;
        user.CanOrganize = canOrganize;
        user.CanInvite = canInvite;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
