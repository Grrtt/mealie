using System.Text.RegularExpressions;
using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Group = Mealie.Domain.Entities.Core.Group;

namespace Mealie.Application.Queries.Admin;

public record GetAllGroupsQuery : IQuery<object>
{
    public async Task<object> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var groups = await services.Db.Groups.IgnoreQueryFilters()
            .Include(g => g.Users).Include(g => g.Households).Include(g => g.Preferences)
            .OrderBy(g => g.Name).ToListAsync(ct);
        var items = groups.Select(AdminGroupMappings.MapGroupToResponse).ToList();
        return new { page = 1, per_page = -1, total = items.Count, total_pages = 1, items };
    }
}

public record GetAdminGroupQuery(Guid GroupId) : IQuery<AdminGroupResponse?>
{
    public async Task<AdminGroupResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var g = await services.Db.Groups.IgnoreQueryFilters()
            .Include(g => g.Users).Include(g => g.Households).Include(g => g.Preferences)
            .FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        return g is null ? null : AdminGroupMappings.MapGroupToResponse(g);
    }
}

public record GetAllHouseholdsQuery : IQuery<object>
{
    public async Task<object> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var households = await services.Db.Households.IgnoreQueryFilters()
            .Include(h => h.Users).Include(h => h.Preferences)
            .OrderBy(h => h.Name).ToListAsync(ct);
        var items = households.Select(AdminGroupMappings.MapHouseholdToResponse).ToList();
        return new { page = 1, per_page = -1, total = items.Count, total_pages = 1, items };
    }
}

public record GetAdminHouseholdQuery(Guid HouseholdId) : IQuery<AdminHouseholdResponse?>
{
    public async Task<AdminHouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var h = await services.Db.Households.IgnoreQueryFilters()
            .Include(h => h.Users).Include(h => h.Preferences)
            .FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        return h is null ? null : AdminGroupMappings.MapHouseholdToResponse(h);
    }
}

file static class AdminGroupMappings
{
    public static string GenerateSlug(string name) =>
        Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public static async Task<string> GenerateUniqueGroupSlugAsync(ApplicationDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var i = 1;
        while (await db.Groups.IgnoreQueryFilters().AnyAsync(g => g.Slug == slug, ct))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    public static async Task<string> GenerateUniqueHouseholdSlugAsync(ApplicationDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var i = 1;
        while (await db.Households.IgnoreQueryFilters().AnyAsync(h => h.Slug == slug, ct))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    public static AdminGroupResponse MapGroupToResponse(Group g) =>
        new()
        {
            Id = g.Id, Name = g.Name, Slug = g.Slug, CreatedAt = g.CreatedAt, UpdateAt = g.UpdateAt,
            UserCount = g.Users.Count, HouseholdCount = g.Households.Count,
            Users = g.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Households = g.Households.Select(h => (object)new { h.Id, h.Name, h.Slug }).ToList(),
            Preferences = g.Preferences is null ? null : new GroupPreferencesDto
            {
                Id = g.Preferences.Id, GroupId = g.Preferences.GroupId,
                PrivateGroup = g.Preferences.PrivateGroup, ShowAnnouncements = false
            }
        };

    public static AdminHouseholdResponse MapHouseholdToResponse(Household h) =>
        new()
        {
            Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId,
            CreatedAt = h.CreatedAt, UpdateAt = h.UpdateAt,
            UserCount = h.Users.Count,
            Users = h.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Webhooks = [],
            Preferences = h.Preferences is null ? null : new HouseholdPreferencesDto
            {
                Id = h.Preferences.Id, HouseholdId = h.Preferences.HouseholdId,
                PrivateHousehold = h.Preferences.PrivateHousehold, ShowAnnouncements = false,
                RecipePublic = bool.TryParse(h.Preferences.RecipePublic, out var rp) && rp,
                RecipeShowNutrition = bool.TryParse(h.Preferences.RecipeShowNutrition, out var rsn) && rsn,
                RecipeShowAssets = bool.TryParse(h.Preferences.RecipeShowAssets, out var rsa) && rsa,
                RecipeLandscapeView = bool.TryParse(h.Preferences.RecipeLandscapeView, out var rlv) && rlv,
                RecipeDisableComments = bool.TryParse(h.Preferences.RecipeDisableComments, out var rdc) && rdc,
                RecipeDisableAmount = bool.TryParse(h.Preferences.RecipeDisableAmount, out var rda) && rda
            }
        };
}
