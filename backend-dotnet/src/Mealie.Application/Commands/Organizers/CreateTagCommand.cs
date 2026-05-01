using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Organizers;

public record CreateTagCommand(Guid GroupId, CreateOrganizerRequest Request) : IQuery<TagResponse>
{
    public async Task<TagResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await TagSlugHelper.EnsureUniqueAsync(db, SlugHelper.Generate(Request.Name), GroupId, ct);
        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = GroupId, CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return new TagResponse
        {
            Id = tag.Id, Name = tag.Name, Slug = tag.Slug, GroupId = tag.GroupId, CreatedAt = tag.CreatedAt,
            UpdateAt = tag.UpdateAt
        };
    }
}

file static class TagSlugHelper
{
    public static async Task<string> EnsureUniqueAsync(ApplicationDbContext db, string slug, Guid groupId,
        CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Tags.IgnoreQueryFilters().AnyAsync(t => t.GroupId == groupId && t.Slug == candidate, ct))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }
}
