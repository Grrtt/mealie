using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetGroupMemberQuery(Guid GroupId, Guid UserId) : IQuery<UserSummaryDto?>
{
    public async Task<UserSummaryDto?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Users.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId && u.Id == UserId)
            .Select(u => new UserSummaryDto
                { Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email })
            .FirstOrDefaultAsync(ct);
    }
}
