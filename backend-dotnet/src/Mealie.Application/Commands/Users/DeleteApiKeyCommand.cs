using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Users;

public record DeleteApiKeyCommand(Guid UserId, int KeyId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == KeyId && k.UserId == UserId, ct);
        if (key is null)
        {
            return false;
        }

        db.ApiKeys.Remove(key);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
