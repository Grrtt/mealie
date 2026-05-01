using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetGroupMembersQuery(Guid GroupId) : IQuery<IList<UserSummaryDto>>
{
    public async Task<IList<UserSummaryDto>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId)
            .Select(u => new UserSummaryDto { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .ToListAsync(ct);
    }
}
