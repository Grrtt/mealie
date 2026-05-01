using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Cookbooks;

public record ReorderCookbooksCommand(Guid HouseholdId, IEnumerable<CookbookReorderRequest> ReorderRequests) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var requests = ReorderRequests.ToList();
        foreach (var req in requests)
        {
            var cookbook = await db.Cookbooks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == req.Id, ct);
            if (cookbook is null) return false;
            cookbook.Position = req.Position;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}