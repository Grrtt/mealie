using Mealie.Application.Dtos.Users;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

public record GetApiKeysQuery(Guid UserId) : IQuery<IList<ApiKeyResponse>>
{
    public async Task<IList<ApiKeyResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.ApiKeys
            .Where(k => k.UserId == UserId)
            .Select(k => new ApiKeyResponse { Id = k.Id, Name = k.Name })
            .ToListAsync(ct);
    }
}
