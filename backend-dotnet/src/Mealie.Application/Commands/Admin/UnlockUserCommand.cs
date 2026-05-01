using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record UnlockUserCommand(Guid UserId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return false;
        user.LockedAt = null;
        user.LoginAttempts = 0;
        await db.SaveChangesAsync(ct);
        return true;
    }
}