using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record DeleteAdminGroupCommand(Guid GroupId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        if (group is null)
        {
            return false;
        }

        db.Groups.Remove(group);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
