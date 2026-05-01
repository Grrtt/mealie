using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Groups;

public class GroupService(ApplicationDbContext db, ILogger<GroupService> logger) : IGroupService
{
    public async Task<GroupResponse?> GetGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return null;
        return new GroupResponse { Id = group.Id, Name = group.Name, Slug = group.Slug };
    }

    public async Task<GroupResponse?> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken ct = default)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return null;
        group.Name = request.Name;
        group.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new GroupResponse { Id = group.Id, Name = group.Name, Slug = group.Slug };
    }

    public async Task<IList<UserSummaryDto>> GetMembersAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == groupId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }

    public async Task<UserSummaryDto?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        return await db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == groupId && u.Id == userId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<HouseholdResponse>> GetHouseholdsAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == groupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }

    public async Task<InviteTokenResponse> CreateInviteTokenAsync(Guid groupId, Guid? householdId, CancellationToken ct = default)
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

    public async Task<IList<InviteTokenResponse>> GetInviteTokensAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.InviteTokens.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId)
            .Select(t => new InviteTokenResponse { Id = t.Id, Token = t.Token, GroupId = t.GroupId, HouseholdId = t.HouseholdId })
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteInviteTokenAsync(Guid groupId, Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.InviteTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.GroupId == groupId, ct);
        if (token is null) return false;
        db.InviteTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<GroupPreferencesResponse?> GetGroupPreferencesAsync(Guid groupId, CancellationToken ct = default)
    {
        var prefs = await db.GroupPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.GroupId == groupId, ct);
        if (prefs is null) return null;
        return MapToGroupPreferencesResponse(prefs);
    }

    public async Task<GroupPreferencesResponse?> UpdateGroupPreferencesAsync(Guid groupId, UpdateGroupPreferencesRequest request, CancellationToken ct = default)
    {
        var prefs = await db.GroupPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.GroupId == groupId, ct);
        if (prefs is null) return null;
        
        if (request.PrivateGroup.HasValue)
            prefs.PrivateGroup = request.PrivateGroup.Value;
        if (request.FirstDayOfWeek is not null)
            prefs.FirstDayOfWeek = request.FirstDayOfWeek;
        
        prefs.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToGroupPreferencesResponse(prefs);
    }

    private static GroupPreferencesResponse MapToGroupPreferencesResponse(GroupPreferences prefs) => new()
    {
        Id = prefs.Id,
        GroupId = prefs.GroupId,
        PrivateGroup = prefs.PrivateGroup,
        FirstDayOfWeek = prefs.FirstDayOfWeek,
    };
}
