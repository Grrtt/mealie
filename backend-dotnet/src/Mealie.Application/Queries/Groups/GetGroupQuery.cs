using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetGroupQuery(Guid GroupId) : IQuery<GroupResponse?>
{
    public async Task<GroupResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var group = await services.Db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == GroupId, ct);
        return group is null ? null : new GroupResponse { Id = group.Id, Name = group.Name, Slug = group.Slug };
    }
}
