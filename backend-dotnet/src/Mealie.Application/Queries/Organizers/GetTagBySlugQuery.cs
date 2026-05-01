using Mealie.Application.Dtos.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetTagBySlugQuery(Guid GroupId, string Slug) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var t = await services.Db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Slug == Slug, ct);
        if (t is null)
        {
            return null;
        }

        return new TagResponse
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt
        };
    }
}
