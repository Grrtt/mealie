using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

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
