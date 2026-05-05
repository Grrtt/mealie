using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Organizers;

public static class OrganizerCrudModule
{
    public static async Task<PaginatedResponse<TagResponse>> GetTagsAsync(
        ApplicationDbContext db,
        Guid groupId,
        PaginationParams pagination,
        string? search,
        CancellationToken ct = default)
    {
        var query = db.Tags.IgnoreQueryFilters().Where(t => t.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search));
        }

        return await ToPageAsync(query.OrderBy(t => t.Name).Select(t => new TagResponse
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            GroupId = t.GroupId,
            CreatedAt = t.CreatedAt,
            UpdateAt = t.UpdateAt
        }), pagination, ct);
    }

    public static async Task<PaginatedResponse<CategoryResponse>> GetCategoriesAsync(
        ApplicationDbContext db,
        Guid groupId,
        PaginationParams pagination,
        string? search,
        CancellationToken ct = default)
    {
        var query = db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        return await ToPageAsync(query.OrderBy(c => c.Name).Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            GroupId = c.GroupId,
            CreatedAt = c.CreatedAt,
            UpdateAt = c.UpdateAt
        }), pagination, ct);
    }

    public static async Task<PaginatedResponse<ToolResponse>> GetToolsAsync(
        ApplicationDbContext db,
        Guid groupId,
        PaginationParams pagination,
        string? search,
        CancellationToken ct = default)
    {
        var query = db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search));
        }

        return await ToPageAsync(query.OrderBy(t => t.Name).Select(t => new ToolResponse
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            GroupId = t.GroupId,
            OnHand = t.OnHand,
            CreatedAt = t.CreatedAt,
            UpdateAt = t.UpdateAt
        }), pagination, ct);
    }

    public static async Task<TagResponse?> GetTagBySlugAsync(
        ApplicationDbContext db,
        Guid groupId,
        string slug,
        CancellationToken ct = default)
    {
        var tag = await db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Slug == slug, ct);
        return tag is null ? null : MapTag(tag);
    }

    public static async Task<CategoryResponse?> GetCategoryBySlugAsync(
        ApplicationDbContext db,
        Guid groupId,
        string slug,
        CancellationToken ct = default)
    {
        var category = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Slug == slug, ct);
        return category is null ? null : MapCategory(category);
    }

    public static async Task<ToolResponse?> GetToolBySlugAsync(
        ApplicationDbContext db,
        Guid groupId,
        string slug,
        CancellationToken ct = default)
    {
        var tool = await db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Slug == slug, ct);
        return tool is null ? null : MapTool(tool);
    }

    public static async Task<TagResponse> CreateTagAsync(
        ApplicationDbContext db,
        Guid groupId,
        CreateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = await OrganizerSlugPolicy.EnsureTagSlugAsync(db, request.Name, groupId, ct: ct),
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return MapTag(tag);
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(
        ApplicationDbContext db,
        Guid groupId,
        CreateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = await OrganizerSlugPolicy.EnsureCategorySlugAsync(db, request.Name, groupId, ct: ct),
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return MapCategory(category);
    }

    public static async Task<ToolResponse> CreateToolAsync(
        ApplicationDbContext db,
        Guid groupId,
        CreateToolRequest request,
        CancellationToken ct = default)
    {
        var tool = new Tool
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = await OrganizerSlugPolicy.EnsureToolSlugAsync(db, request.Name, groupId, ct: ct),
            GroupId = groupId,
            OnHand = request.OnHand,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Tools.Add(tool);
        await db.SaveChangesAsync(ct);
        return MapTool(tool);
    }

    public static async Task<TagResponse?> UpdateTagAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        UpdateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        if (tag is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            tag.Name = request.Name;
            tag.Slug = await OrganizerSlugPolicy.EnsureTagSlugAsync(db, request.Name, groupId, id, ct);
        }

        tag.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapTag(tag);
    }

    public static async Task<CategoryResponse?> UpdateCategoryAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        UpdateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var category = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Id == id, ct);
        if (category is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            category.Name = request.Name;
            category.Slug = await OrganizerSlugPolicy.EnsureCategorySlugAsync(db, request.Name, groupId, id, ct);
        }

        category.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapCategory(category);
    }

    public static async Task<ToolResponse?> UpdateToolAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        UpdateToolRequest request,
        CancellationToken ct = default)
    {
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        if (tool is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            tool.Name = request.Name;
            tool.Slug = await OrganizerSlugPolicy.EnsureToolSlugAsync(db, request.Name, groupId, id, ct);
        }

        if (request.OnHand.HasValue)
        {
            tool.OnHand = request.OnHand.Value;
        }

        tool.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapTool(tool);
    }

    public static Task<bool> DeleteTagAsync(ApplicationDbContext db, Guid groupId, Guid id, CancellationToken ct = default)
    {
        return DeleteAsync(db.Tags.IgnoreQueryFilters().Where(t => t.GroupId == groupId), tag => db.Tags.Remove(tag), id, db, ct);
    }

    public static Task<bool> DeleteCategoryAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        CancellationToken ct = default)
    {
        return DeleteAsync(
            db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == groupId),
            category => db.Categories.Remove(category),
            id,
            db,
            ct);
    }

    public static Task<bool> DeleteToolAsync(ApplicationDbContext db, Guid groupId, Guid id, CancellationToken ct = default)
    {
        return DeleteAsync(db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == groupId), tool => db.Tools.Remove(tool), id, db, ct);
    }

    public static async Task<IList<TagResponse>> GetEmptyTagsAsync(ApplicationDbContext db, Guid groupId, CancellationToken ct = default)
    {
        return await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId && !t.Recipes.Any())
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                GroupId = t.GroupId,
                CreatedAt = t.CreatedAt,
                UpdateAt = t.UpdateAt
            })
            .ToListAsync(ct);
    }

    public static async Task<IList<CategoryResponse>> GetEmptyCategoriesAsync(
        ApplicationDbContext db,
        Guid groupId,
        CancellationToken ct = default)
    {
        return await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == groupId && !c.Recipes.Any())
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                GroupId = c.GroupId,
                CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt
            })
            .ToListAsync(ct);
    }

    public static Task<IList<RecipeSummaryResponse>> GetRecipesByTagAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid tagId,
        CancellationToken ct = default)
    {
        return GetRecipesByTagInternalAsync(db, groupId, tagId, ct);
    }

    public static Task<IList<RecipeSummaryResponse>> GetRecipesByCategoryAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid categoryId,
        CancellationToken ct = default)
    {
        return GetRecipesByCategoryInternalAsync(db, groupId, categoryId, ct);
    }

    public static Task<IList<RecipeSummaryResponse>> GetRecipesByToolAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid toolId,
        CancellationToken ct = default)
    {
        return GetRecipesByToolInternalAsync(db, groupId, toolId, ct);
    }

    public static TagResponse MapTag(Tag tag)
    {
        return new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            GroupId = tag.GroupId,
            CreatedAt = tag.CreatedAt,
            UpdateAt = tag.UpdateAt
        };
    }

    public static CategoryResponse MapCategory(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            GroupId = category.GroupId,
            CreatedAt = category.CreatedAt,
            UpdateAt = category.UpdateAt
        };
    }

    public static ToolResponse MapTool(Tool tool)
    {
        return new ToolResponse
        {
            Id = tool.Id,
            Name = tool.Name,
            Slug = tool.Slug,
            GroupId = tool.GroupId,
            OnHand = tool.OnHand,
            CreatedAt = tool.CreatedAt,
            UpdateAt = tool.UpdateAt
        };
    }

    private static async Task<PaginatedResponse<TResponse>> ToPageAsync<TResponse>(
        IQueryable<TResponse> query,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip(pagination.Skip).Take(pagination.PerPage).ToListAsync(ct);
        return new PaginatedResponse<TResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        };
    }

    private static async Task<bool> DeleteAsync<TEntity>(IQueryable<TEntity> query, Action<TEntity> remove,
        Guid id, ApplicationDbContext db, CancellationToken ct)
        where TEntity : class
    {
        var entity = await query.FirstOrDefaultAsync(BuildIdPredicate<TEntity>(id), ct);
        if (entity is null)
        {
            return false;
        }

        remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<IList<RecipeSummaryResponse>> GetRecipesByTagInternalAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        CancellationToken ct)
    {
        var entity = await db.Tags.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        return entity is null ? [] : entity.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }

    private static async Task<IList<RecipeSummaryResponse>> GetRecipesByCategoryInternalAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        CancellationToken ct)
    {
        var entity = await db.Categories.IgnoreQueryFilters()
            .Include(c => c.Recipes).ThenInclude(r => r.Tags)
            .Include(c => c.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Id == id, ct);
        return entity is null ? [] : entity.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }

    private static async Task<IList<RecipeSummaryResponse>> GetRecipesByToolInternalAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        CancellationToken ct)
    {
        var entity = await db.Tools.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        return entity is null ? [] : entity.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }

    private static System.Linq.Expressions.Expression<Func<TEntity, bool>> BuildIdPredicate<TEntity>(Guid id)
    {
        var entity = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "entity");
        var body = System.Linq.Expressions.Expression.Equal(
            System.Linq.Expressions.Expression.PropertyOrField(entity, "Id"),
            System.Linq.Expressions.Expression.Constant(id));
        return System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }
}
