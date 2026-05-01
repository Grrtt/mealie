using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetCategoriesQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<CategoryResponse>>
{
    public async Task<PaginatedResponse<CategoryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(c => c.Name.Contains(Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(c => new CategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt })
            .ToListAsync(ct);
        return new PaginatedResponse<CategoryResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}

public record GetCategoryBySlugQuery(Guid GroupId, string Slug) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var c = await services.Db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Slug == Slug, ct);
        if (c is null) return null;
        return new CategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt };
    }
}

public record GetEmptyCategoriesQuery(Guid GroupId) : IQuery<IList<CategoryResponse>>
{
    public async Task<IList<CategoryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == GroupId && !c.Recipes.Any())
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt })
            .ToListAsync(ct);
    }
}

public record GetRecipesByCategoryQuery(Guid GroupId, Guid CategoryId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var category = await services.Db.Categories.IgnoreQueryFilters()
            .Include(c => c.Recipes).ThenInclude(r => r.Tags)
            .Include(c => c.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Id == CategoryId, ct);
        if (category is null) return [];
        return category.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }
}
