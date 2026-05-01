using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Cookbooks;

public record DeleteCookbookCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == Id, ct);
        if (cookbook is null) return false;
        db.Cookbooks.Remove(cookbook);
        await db.SaveChangesAsync(ct);
        return true;
    }
}