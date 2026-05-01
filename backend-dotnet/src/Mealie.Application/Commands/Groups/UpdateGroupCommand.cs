using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

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