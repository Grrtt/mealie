using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record DeleteGroupInviteTokenCommand(Guid GroupId, Guid TokenId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var token = await db.InviteTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == TokenId && t.GroupId == GroupId, ct);
        if (token is null)
        {
            return false;
        }

        db.InviteTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
