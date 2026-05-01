using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetGroupQuery(Guid GroupId) : IQuery<GroupResponse?>
{
    public async Task<GroupResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var group = await services.Db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        return group is null ? null : new GroupResponse { Id = group.Id, Name = group.Name, Slug = group.Slug };
    }
}

public record UpdateGroupCommand(Guid GroupId, UpdateGroupRequest Request) : IQuery<GroupResponse?>
{
    public async Task<GroupResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        if (group is null) return null;
        group.Name = Request.Name;
        group.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new GroupResponse { Id = group.Id, Name = group.Name, Slug = group.Slug };
    }
}

public record GetGroupMembersQuery(Guid GroupId) : IQuery<IList<UserSummaryDto>>
{
    public async Task<IList<UserSummaryDto>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }
}

public record GetGroupMemberQuery(Guid GroupId, Guid UserId) : IQuery<UserSummaryDto?>
{
    public async Task<UserSummaryDto?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId && u.Id == UserId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .FirstOrDefaultAsync(ct);
    }
}

public record GetGroupHouseholdsQuery(Guid GroupId) : IQuery<IList<HouseholdResponse>>
{
    public async Task<IList<HouseholdResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == GroupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }
}

public record CreateGroupInviteTokenCommand(Guid GroupId, Guid? HouseholdId) : IQuery<InviteTokenResponse>
{
    public async Task<InviteTokenResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var token = new GroupInviteToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            GroupId = GroupId, HouseholdId = HouseholdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.InviteTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return new InviteTokenResponse { Id = token.Id, Token = token.Token, GroupId = GroupId, HouseholdId = HouseholdId };
    }
}

public record GetGroupInviteTokensQuery(Guid GroupId) : IQuery<IList<InviteTokenResponse>>
{
    public async Task<IList<InviteTokenResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.InviteTokens.IgnoreQueryFilters()
            .Where(t => t.GroupId == GroupId)
            .Select(t => new InviteTokenResponse { Id = t.Id, Token = t.Token, GroupId = t.GroupId, HouseholdId = t.HouseholdId })
            .ToListAsync(ct);
    }
}

public record DeleteGroupInviteTokenCommand(Guid GroupId, Guid TokenId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var token = await db.InviteTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == TokenId && t.GroupId == GroupId, ct);
        if (token is null) return false;
        db.InviteTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record GetGroupPreferencesQuery(Guid GroupId) : IQuery<GroupPreferencesResponse?>
{
    public async Task<GroupPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var prefs = await services.Db.GroupPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.GroupId == GroupId, ct);
        return prefs is null ? null : GroupMappings.MapToGroupPreferencesResponse(prefs);
    }
}

public record UpdateGroupPreferencesCommand(Guid GroupId, UpdateGroupPreferencesRequest Request) : IQuery<GroupPreferencesResponse?>
{
    public async Task<GroupPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var prefs = await db.GroupPreferences.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.GroupId == GroupId, ct);
        if (prefs is null) return null;
        if (Request.PrivateGroup.HasValue) prefs.PrivateGroup = Request.PrivateGroup.Value;
        if (Request.FirstDayOfWeek is not null) prefs.FirstDayOfWeek = Request.FirstDayOfWeek;
        prefs.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return GroupMappings.MapToGroupPreferencesResponse(prefs);
    }
}

file static class GroupMappings
{
    public static GroupPreferencesResponse MapToGroupPreferencesResponse(GroupPreferences prefs) =>
        new()
        {
            Id = prefs.Id, GroupId = prefs.GroupId,
            PrivateGroup = prefs.PrivateGroup, FirstDayOfWeek = prefs.FirstDayOfWeek
        };
}
