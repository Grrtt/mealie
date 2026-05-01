using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record DeleteToolCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tool is null) return false;
        db.Tools.Remove(tool);
        await db.SaveChangesAsync(ct);
        return true;
    }
}