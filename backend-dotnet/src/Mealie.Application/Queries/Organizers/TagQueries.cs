using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
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
