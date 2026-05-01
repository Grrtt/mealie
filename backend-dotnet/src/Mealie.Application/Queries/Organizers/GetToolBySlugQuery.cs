using Mealie.Application.Dtos.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetToolBySlugQuery(Guid GroupId, string Slug) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var t = await services.Db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Slug == Slug, ct);
        if (t is null)
        {
            return null;
        }

        return new ToolResponse
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, OnHand = t.OnHand, CreatedAt = t.CreatedAt,
            UpdateAt = t.UpdateAt
        };
    }
}
