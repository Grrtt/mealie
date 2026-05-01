using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetHouseholdsForGroupQuery(Guid GroupId) : IQuery<IList<HouseholdResponse>>
{
    public async Task<IList<HouseholdResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == GroupId)
            .Select(h => new HouseholdResponse { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId })
            .ToListAsync(ct);
    }
}
