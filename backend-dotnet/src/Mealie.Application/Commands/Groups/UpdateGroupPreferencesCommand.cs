using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

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
    public static Mealie.Application.Dtos.Groups.GroupPreferencesResponse MapToGroupPreferencesResponse(Mealie.Domain.Entities.Settings.GroupPreferences prefs) =>
        new()
        {
            Id = prefs.Id, GroupId = prefs.GroupId,
            PrivateGroup = prefs.PrivateGroup, FirstDayOfWeek = prefs.FirstDayOfWeek
        };
}