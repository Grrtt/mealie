using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record DeleteCategoryCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Id == Id, ct);
        if (cat is null) return false;
        db.Categories.Remove(cat);
        await db.SaveChangesAsync(ct);
        return true;
    }
}