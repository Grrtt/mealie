using Mealie.Application.Dtos.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetEmptyCategoriesQuery(Guid GroupId) : IQuery<IList<CategoryResponse>>
{
    public async Task<IList<CategoryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == GroupId && !c.Recipes.Any())
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt
            })
            .ToListAsync(ct);
    }
}
