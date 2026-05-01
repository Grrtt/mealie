using Mealie.Application.Dtos.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetEmptyTagsQuery(Guid GroupId) : IQuery<IList<TagResponse>>
{
    public async Task<IList<TagResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == GroupId && !t.Recipes.Any())
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse
            {
                Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt,
                UpdateAt = t.UpdateAt
            })
            .ToListAsync(ct);
    }
}
