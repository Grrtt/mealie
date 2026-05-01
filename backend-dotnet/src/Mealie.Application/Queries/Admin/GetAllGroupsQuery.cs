using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
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

file static class AdminGroupMappings
{
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
