using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetGroupInviteTokensQuery(Guid GroupId) : IQuery<IList<InviteTokenResponse>>
{
    public async Task<IList<InviteTokenResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.InviteTokens.IgnoreQueryFilters()
            .Where(t => t.GroupId == GroupId)
            .Select(t => new InviteTokenResponse
                { Id = t.Id, Token = t.Token, GroupId = t.GroupId, HouseholdId = t.HouseholdId })
            .ToListAsync(ct);
    }
}
