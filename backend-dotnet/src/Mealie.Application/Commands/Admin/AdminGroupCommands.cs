using System.Text.RegularExpressions;
using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;
using Group = Mealie.Domain.Entities.Core.Group;

namespace Mealie.Application.Commands.Admin;

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
}
