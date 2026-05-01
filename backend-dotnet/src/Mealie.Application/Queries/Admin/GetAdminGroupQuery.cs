using Mealie.Application.Dtos.Admin;
using Microsoft.EntityFrameworkCore;
using Group = Mealie.Domain.Entities.Core.Group;

namespace Mealie.Application.Queries.Admin;

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

file static class AdminGroupMappings
{
    public static AdminGroupResponse MapGroupToResponse(Group g)
    {
        return new AdminGroupResponse
        {
            Id = g.Id, Name = g.Name, Slug = g.Slug, CreatedAt = g.CreatedAt, UpdateAt = g.UpdateAt,
            UserCount = g.Users.Count, HouseholdCount = g.Households.Count,
            Users = g.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Households = g.Households.Select(h => (object)new { h.Id, h.Name, h.Slug }).ToList(),
            Preferences = g.Preferences is null
                ? null
                : new GroupPreferencesDto
                {
                    Id = g.Preferences.Id, GroupId = g.Preferences.GroupId,
                    PrivateGroup = g.Preferences.PrivateGroup, ShowAnnouncements = false
                }
        };
    }
}
