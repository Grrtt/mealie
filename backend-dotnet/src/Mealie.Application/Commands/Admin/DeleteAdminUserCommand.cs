using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record DeleteAdminUserCommand(Guid UserId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }
}