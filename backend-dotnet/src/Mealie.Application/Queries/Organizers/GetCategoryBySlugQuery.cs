using Mealie.Application.Dtos.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetCategoryBySlugQuery(Guid GroupId, string Slug) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var c = await services.Db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Slug == Slug, ct);
        if (c is null)
        {
            return null;
        }

        return new CategoryResponse
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
    }
}
