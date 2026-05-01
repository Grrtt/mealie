using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record UpdateCategoryCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cat = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Id == Id, ct);
        if (cat is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            cat.Name = Request.Name;
            cat.Slug = SlugHelper.Generate(Request.Name);
        }

        cat.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CategoryResponse
        {
            Id = cat.Id, Name = cat.Name, Slug = cat.Slug, GroupId = cat.GroupId, CreatedAt = cat.CreatedAt,
            UpdateAt = cat.UpdateAt
        };
    }
}
