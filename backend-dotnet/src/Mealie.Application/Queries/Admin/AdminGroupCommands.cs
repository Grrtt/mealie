using System.Text.RegularExpressions;
using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Group = Mealie.Domain.Entities.Core.Group;

namespace Mealie.Application.Queries.Admin;

public record CreateAdminGroupCommand(CreateAdminGroupRequest Request) : IQuery<AdminGroupResponse>
{
    public async Task<AdminGroupResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await AdminGroupMappings.GenerateUniqueGroupSlugAsync(db, Request.Name, ct);
        var group = new Group
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);
        return AdminGroupMappings.MapGroupToResponse(group);
    }
}

public record UpdateAdminGroupCommand(Guid GroupId, UpdateAdminGroupRequest Request) : IQuery<AdminGroupResponse?>
{
    public async Task<AdminGroupResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var group = await db.Groups.IgnoreQueryFilters()
            .Include(g => g.Users).Include(g => g.Households).Include(g => g.Preferences)
            .FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        if (group is null) return null;

        if (Request.Name is not null)
        {
            group.Name = Request.Name;
            group.Slug = AdminGroupMappings.GenerateSlug(Request.Name);
        }

        if (Request.Preferences is not null && group.Preferences is not null)
        {
            if (Request.Preferences.PrivateGroup.HasValue)
                group.Preferences.PrivateGroup = Request.Preferences.PrivateGroup.Value;
            group.Preferences.UpdateAt = DateTime.UtcNow;
        }

        group.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return AdminGroupMappings.MapGroupToResponse(group);
    }
}

public record DeleteAdminGroupCommand(Guid GroupId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        if (group is null) return false;
        db.Groups.Remove(group);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record CreateAdminHouseholdCommand(CreateAdminHouseholdRequest Request) : IQuery<AdminHouseholdResponse>
{
    public async Task<AdminHouseholdResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await AdminGroupMappings.GenerateUniqueHouseholdSlugAsync(db, Request.Name, ct);
        var household = new Household
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = Request.GroupId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return AdminGroupMappings.MapHouseholdToResponse(household);
    }
}

public record UpdateAdminHouseholdCommand(Guid HouseholdId, UpdateAdminHouseholdRequest Request)
    : IQuery<AdminHouseholdResponse?>
{
    public async Task<AdminHouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var h = await db.Households.IgnoreQueryFilters()
            .Include(h => h.Users).Include(h => h.Preferences)
            .FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        if (h is null) return null;

        if (Request.Name is not null)
        {
            h.Name = Request.Name;
            h.Slug = AdminGroupMappings.GenerateSlug(Request.Name);
        }

        if (Request.Preferences is not null && h.Preferences is not null)
        {
            if (Request.Preferences.PrivateHousehold.HasValue)
                h.Preferences.PrivateHousehold = Request.Preferences.PrivateHousehold.Value;
            if (Request.Preferences.RecipePublic.HasValue)
                h.Preferences.RecipePublic = Request.Preferences.RecipePublic.Value.ToString().ToLower();
            if (Request.Preferences.RecipeShowNutrition.HasValue)
                h.Preferences.RecipeShowNutrition = Request.Preferences.RecipeShowNutrition.Value.ToString().ToLower();
            if (Request.Preferences.RecipeShowAssets.HasValue)
                h.Preferences.RecipeShowAssets = Request.Preferences.RecipeShowAssets.Value.ToString().ToLower();
            if (Request.Preferences.RecipeLandscapeView.HasValue)
                h.Preferences.RecipeLandscapeView = Request.Preferences.RecipeLandscapeView.Value.ToString().ToLower();
            if (Request.Preferences.RecipeDisableComments.HasValue)
                h.Preferences.RecipeDisableComments = Request.Preferences.RecipeDisableComments.Value.ToString().ToLower();
            if (Request.Preferences.RecipeDisableAmount.HasValue)
                h.Preferences.RecipeDisableAmount = Request.Preferences.RecipeDisableAmount.Value.ToString().ToLower();
            h.Preferences.UpdateAt = DateTime.UtcNow;
        }

        h.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return AdminGroupMappings.MapHouseholdToResponse(h);
    }
}

public record DeleteAdminHouseholdCommand(Guid HouseholdId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        if (h is null) return false;
        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
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
