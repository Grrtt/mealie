using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record UpdateTagCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tag is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            tag.Name = Request.Name;
            tag.Slug = SlugHelper.Generate(Request.Name);
        }

        tag.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new TagResponse
        {
            Id = tag.Id, Name = tag.Name, Slug = tag.Slug, GroupId = tag.GroupId, CreatedAt = tag.CreatedAt,
            UpdateAt = tag.UpdateAt
        };
    }
}
