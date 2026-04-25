using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Admin;

public class AdminGroupService(ApplicationDbContext db) : IAdminGroupService
{
    // ── Groups ──────────────────────────────────────────────────────────────

    public async Task<IList<AdminGroupResponse>> GetAllGroupsAsync(CancellationToken ct = default)
    {
        return await db.Groups.IgnoreQueryFilters()
            .Select(g => new AdminGroupResponse
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                CreatedAt = g.CreatedAt,
                UpdateAt = g.UpdateAt,
                UserCount = g.Users.Count,
                HouseholdCount = g.Households.Count,
            })
            .ToListAsync(ct);
    }

    public async Task<AdminGroupResponse?> GetGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var g = await db.Groups.IgnoreQueryFilters()
            .Include(g => g.Users)
            .Include(g => g.Households)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        return g is null ? null : MapGroupToResponse(g);
    }

    public async Task<AdminGroupResponse?> CreateGroupAsync(CreateAdminGroupRequest request, CancellationToken ct = default)
    {
        var slug = GenerateSlug(request.Name);
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);
        return MapGroupToResponse(group);
    }

    public async Task<AdminGroupResponse?> UpdateGroupAsync(Guid groupId, UpdateAdminGroupRequest request, CancellationToken ct = default)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .Include(g => g.Users)
            .Include(g => g.Households)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return null;

        if (request.Name is not null)
        {
            group.Name = request.Name;
            group.Slug = GenerateSlug(request.Name);
        }
        group.UpdateAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapGroupToResponse(group);
    }

    public async Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return false;

        db.Groups.Remove(group);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Households ──────────────────────────────────────────────────────────

    public async Task<IList<AdminHouseholdResponse>> GetAllHouseholdsAsync(CancellationToken ct = default)
    {
        return await db.Households.IgnoreQueryFilters()
            .Select(h => new AdminHouseholdResponse
            {
                Id = h.Id,
                Name = h.Name,
                Slug = h.Slug,
                GroupId = h.GroupId,
                CreatedAt = h.CreatedAt,
                UpdateAt = h.UpdateAt,
                UserCount = h.Users.Count,
            })
            .ToListAsync(ct);
    }

    public async Task<AdminHouseholdResponse?> GetHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters()
            .Include(h => h.Users)
            .FirstOrDefaultAsync(h => h.Id == householdId, ct);
        return h is null ? null : MapHouseholdToResponse(h);
    }

    public async Task<AdminHouseholdResponse?> CreateHouseholdAsync(CreateAdminHouseholdRequest request, CancellationToken ct = default)
    {
        var slug = GenerateSlug(request.Name);
        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            GroupId = request.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return MapHouseholdToResponse(household);
    }

    public async Task<AdminHouseholdResponse?> UpdateHouseholdAsync(Guid householdId, UpdateAdminHouseholdRequest request, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters()
            .Include(h => h.Users)
            .FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return null;

        if (request.Name is not null)
        {
            h.Name = request.Name;
            h.Slug = GenerateSlug(request.Name);
        }
        h.UpdateAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapHouseholdToResponse(h);
    }

    public async Task<bool> DeleteHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == householdId, ct);
        if (h is null) return false;

        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateSlug(string name)
        => System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    private static AdminGroupResponse MapGroupToResponse(Group g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Slug = g.Slug,
        CreatedAt = g.CreatedAt,
        UpdateAt = g.UpdateAt,
        UserCount = g.Users.Count,
        HouseholdCount = g.Households.Count,
    };

    private static AdminHouseholdResponse MapHouseholdToResponse(Household h) => new()
    {
        Id = h.Id,
        Name = h.Name,
        Slug = h.Slug,
        GroupId = h.GroupId,
        CreatedAt = h.CreatedAt,
        UpdateAt = h.UpdateAt,
        UserCount = h.Users.Count,
    };
}
