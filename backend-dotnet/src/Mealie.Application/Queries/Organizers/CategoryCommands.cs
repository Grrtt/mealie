using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record CreateCategoryCommand(Guid GroupId, CreateOrganizerRequest Request) : IQuery<CategoryResponse>
{
    public async Task<CategoryResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await CategorySlugHelper.EnsureUniqueAsync(db, SlugHelper.Generate(Request.Name), GroupId, ct);
        var cat = new Category { Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync(ct);
        return new CategoryResponse { Id = cat.Id, Name = cat.Name, Slug = cat.Slug, GroupId = cat.GroupId, CreatedAt = cat.CreatedAt, UpdateAt = cat.UpdateAt };
    }
}

public record UpdateCategoryCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Id == Id, ct);
        if (cat is null) return null;
        if (Request.Name is not null) { cat.Name = Request.Name; cat.Slug = SlugHelper.Generate(Request.Name); }
        cat.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CategoryResponse { Id = cat.Id, Name = cat.Name, Slug = cat.Slug, GroupId = cat.GroupId, CreatedAt = cat.CreatedAt, UpdateAt = cat.UpdateAt };
    }
}

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

file static class CategorySlugHelper
{
    public static async Task<string> EnsureUniqueAsync(ApplicationDbContext db, string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Categories.IgnoreQueryFilters().AnyAsync(c => c.GroupId == groupId && c.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }
}
