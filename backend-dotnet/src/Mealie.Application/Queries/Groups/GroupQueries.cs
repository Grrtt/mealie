using Mealie.Application.Dtos.Groups;
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

public record GetGroupPreferencesQuery(Guid GroupId) : IQuery<GroupPreferencesResponse?>
{
    public async Task<GroupPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var prefs = await services.Db.GroupPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.GroupId == GroupId, ct);
        return prefs is null ? null : GroupMappings.MapToGroupPreferencesResponse(prefs);
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
