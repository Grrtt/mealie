using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Group = Mealie.Domain.Entities.Core.Group;

namespace Mealie.Application.Commands.Admin;

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

file static class AdminGroupMappings
{
    public static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public static async Task<string> GenerateUniqueGroupSlugAsync(Mealie.Infrastructure.Data.ApplicationDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var i = 1;
        while (await db.Groups.IgnoreQueryFilters().AnyAsync(g => g.Slug == slug, ct))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    public static Mealie.Application.Dtos.Admin.AdminGroupResponse MapGroupToResponse(Mealie.Domain.Entities.Core.Group g) =>
        new()
        {
            Id = g.Id, Name = g.Name, Slug = g.Slug, CreatedAt = g.CreatedAt, UpdateAt = g.UpdateAt,
            UserCount = g.Users.Count, HouseholdCount = g.Households.Count,
            Users = g.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Households = g.Households.Select(h => (object)new { h.Id, h.Name, h.Slug }).ToList(),
            Preferences = g.Preferences is null ? null : new Mealie.Application.Dtos.Admin.GroupPreferencesDto
            {
                Id = g.Preferences.Id, GroupId = g.Preferences.GroupId,
                PrivateGroup = g.Preferences.PrivateGroup, ShowAnnouncements = false
            }
        };
}