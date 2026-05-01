using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetTagsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<TagResponse>>
{
    public async Task<PaginatedResponse<TagResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Tags.IgnoreQueryFilters().Where(t => t.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(t => t.Name.Contains(Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(t => new TagResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt })
            .ToListAsync(ct);
        return new PaginatedResponse<TagResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}

public record GetTagBySlugQuery(Guid GroupId, string Slug) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var t = await services.Db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Slug == Slug, ct);
        if (t is null) return null;
        return new TagResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt };
    }
}

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

public record GetEmptyTagsQuery(Guid GroupId) : IQuery<IList<TagResponse>>
{
    public async Task<IList<TagResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == GroupId && !t.Recipes.Any())
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse { Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt })
            .ToListAsync(ct);
    }
}

public record GetRecipesByTagQuery(Guid GroupId, Guid TagId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var tag = await services.Db.Tags.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == TagId, ct);
        if (tag is null) return [];
        return tag.Recipes.Select(RecipeMappings.MapToSummary).ToList();
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
