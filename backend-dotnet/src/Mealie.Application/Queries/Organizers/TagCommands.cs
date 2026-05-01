using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record CreateTagCommand(Guid GroupId, CreateOrganizerRequest Request) : IQuery<TagResponse>
{
    public async Task<TagResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await TagSlugHelper.EnsureUniqueAsync(db, SlugHelper.Generate(Request.Name), GroupId, ct);
        var tag = new Tag { Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return new TagResponse { Id = tag.Id, Name = tag.Name, Slug = tag.Slug, GroupId = tag.GroupId, CreatedAt = tag.CreatedAt, UpdateAt = tag.UpdateAt };
    }
}

public record UpdateTagCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tag is null) return null;
        if (Request.Name is not null) { tag.Name = Request.Name; tag.Slug = SlugHelper.Generate(Request.Name); }
        tag.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new TagResponse { Id = tag.Id, Name = tag.Name, Slug = tag.Slug, GroupId = tag.GroupId, CreatedAt = tag.CreatedAt, UpdateAt = tag.UpdateAt };
    }
}

public record DeleteTagCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == Id, ct);
        if (tag is null) return false;
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

file static class TagSlugHelper
{
    public static async Task<string> EnsureUniqueAsync(ApplicationDbContext db, string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Tags.IgnoreQueryFilters().AnyAsync(t => t.GroupId == groupId && t.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }
}
