using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.Groups;

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
