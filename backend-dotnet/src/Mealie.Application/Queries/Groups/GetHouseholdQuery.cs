using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetHouseholdQuery(Guid HouseholdId) : IQuery<HouseholdResponse?>
{
    public async Task<HouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var h = await services.Db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        return h is null ? null : HouseholdMappings.MapToResponse(h);
    }
}

file static class HouseholdMappings
{
    public static HouseholdResponse MapToResponse(Household h) =>
        new() { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId };
}
