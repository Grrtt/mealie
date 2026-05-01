using Mealie.Application.Common;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Organizers;

public class OrganizerService(ApplicationDbContext db) : IOrganizerService
{
    // ── Tags ────────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<TagResponse>> GetTagsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default)
    {
        var query = db.Tags.IgnoreQueryFilters().Where(t => t.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(t => new TagResponse
            {
                Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt,
                UpdateAt = t.UpdateAt
            })
            .ToListAsync(ct);
        return new PaginatedResponse<TagResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items
        };
    }

    public async Task<TagResponse?> GetTagBySlugAsync(Guid groupId, string slug, CancellationToken ct = default)
    {
        var t = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == groupId && t.Slug == slug, ct);
        if (t is null)
        {
            return null;
        }

        return new TagResponse
        {
            Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt
        };
    }

    public async Task<TagResponse> CreateTagAsync(Guid groupId, CreateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var slug = await EnsureUniqueTagSlugAsync(SlugHelper.Generate(request.Name), groupId, ct);
        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = request.Name, Slug = slug, GroupId = groupId, CreatedAt = DateTime.UtcNow,
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

    public async Task<TagResponse?> UpdateTagAsync(Guid groupId, Guid id, UpdateOrganizerRequest request,
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
            tag.Slug = SlugHelper.Generate(request.Name);
        }

        tag.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new TagResponse
        {
            Id = tag.Id, Name = tag.Name, Slug = tag.Slug, GroupId = tag.GroupId, CreatedAt = tag.CreatedAt,
            UpdateAt = tag.UpdateAt
        };
    }

    public async Task<bool> DeleteTagAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        if (tag is null)
        {
            return false;
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<TagResponse>> GetEmptyTagsAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == groupId && !t.Recipes.Any())
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse
            {
                Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, CreatedAt = t.CreatedAt,
                UpdateAt = t.UpdateAt
            })
            .ToListAsync(ct);
    }

    public async Task<IList<RecipeSummaryResponse>> GetRecipesByTagAsync(Guid groupId, Guid tagId,
        CancellationToken ct = default)
    {
        var tag = await db.Tags.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == tagId, ct);

        if (tag is null)
        {
            return [];
        }

        return tag.Recipes.Select(MapRecipeSummary).ToList();
    }

    // ── Categories ──────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<CategoryResponse>> GetCategoriesAsync(Guid groupId,
        PaginationParams pagination, string? search = null, CancellationToken ct = default)
    {
        var query = db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(c => new CategoryResponse
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt
            })
            .ToListAsync(ct);
        return new PaginatedResponse<CategoryResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items
        };
    }

    public async Task<CategoryResponse?> GetCategoryBySlugAsync(Guid groupId, string slug,
        CancellationToken ct = default)
    {
        var c = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Slug == slug, ct);
        if (c is null)
        {
            return null;
        }

        return new CategoryResponse
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
    }

    public async Task<CategoryResponse> CreateCategoryAsync(Guid groupId, CreateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var slug = await EnsureUniqueCategorySlugAsync(SlugHelper.Generate(request.Name), groupId, ct);
        var cat = new Category
        {
            Id = Guid.NewGuid(), Name = request.Name, Slug = slug, GroupId = groupId, CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync(ct);
        return new CategoryResponse
        {
            Id = cat.Id, Name = cat.Name, Slug = cat.Slug, GroupId = cat.GroupId, CreatedAt = cat.CreatedAt,
            UpdateAt = cat.UpdateAt
        };
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(Guid groupId, Guid id, UpdateOrganizerRequest request,
        CancellationToken ct = default)
    {
        var cat = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Id == id, ct);
        if (cat is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            cat.Name = request.Name;
            cat.Slug = SlugHelper.Generate(request.Name);
        }

        cat.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CategoryResponse
        {
            Id = cat.Id, Name = cat.Name, Slug = cat.Slug, GroupId = cat.GroupId, CreatedAt = cat.CreatedAt,
            UpdateAt = cat.UpdateAt
        };
    }

    public async Task<bool> DeleteCategoryAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var cat = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Id == id, ct);
        if (cat is null)
        {
            return false;
        }

        db.Categories.Remove(cat);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<CategoryResponse>> GetEmptyCategoriesAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == groupId && !c.Recipes.Any())
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug, GroupId = c.GroupId, CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt
            })
            .ToListAsync(ct);
    }

    public async Task<IList<RecipeSummaryResponse>> GetRecipesByCategoryAsync(Guid groupId, Guid categoryId,
        CancellationToken ct = default)
    {
        var category = await db.Categories.IgnoreQueryFilters()
            .Include(c => c.Recipes).ThenInclude(r => r.Tags)
            .Include(c => c.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Id == categoryId, ct);

        if (category is null)
        {
            return [];
        }

        return category.Recipes.Select(MapRecipeSummary).ToList();
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<ToolResponse>> GetToolsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default)
    {
        var query = db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(t => new ToolResponse
            {
                Id = t.Id, Name = t.Name, Slug = t.Slug, GroupId = t.GroupId, OnHand = t.OnHand,
                CreatedAt = t.CreatedAt, UpdateAt = t.UpdateAt
            })
            .ToListAsync(ct);
        return new PaginatedResponse<ToolResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items
        };
    }

    public async Task<ToolResponse?> GetToolBySlugAsync(Guid groupId, string slug, CancellationToken ct = default)
    {
        var t = await db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Slug == slug, ct);
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

    public async Task<ToolResponse> CreateToolAsync(Guid groupId, CreateToolRequest request,
        CancellationToken ct = default)
    {
        var slug = await EnsureUniqueToolSlugAsync(SlugHelper.Generate(request.Name), groupId, ct);
        var tool = new Tool
        {
            Id = Guid.NewGuid(), Name = request.Name, Slug = slug, GroupId = groupId, OnHand = request.OnHand,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Tools.Add(tool);
        await db.SaveChangesAsync(ct);
        return new ToolResponse
        {
            Id = tool.Id, Name = tool.Name, Slug = tool.Slug, GroupId = tool.GroupId, OnHand = tool.OnHand,
            CreatedAt = tool.CreatedAt, UpdateAt = tool.UpdateAt
        };
    }

    public async Task<ToolResponse?> UpdateToolAsync(Guid groupId, Guid id, UpdateToolRequest request,
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
            tool.Slug = SlugHelper.Generate(request.Name);
        }

        if (request.OnHand.HasValue)
        {
            tool.OnHand = request.OnHand.Value;
        }

        tool.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ToolResponse
        {
            Id = tool.Id, Name = tool.Name, Slug = tool.Slug, GroupId = tool.GroupId, OnHand = tool.OnHand,
            CreatedAt = tool.CreatedAt, UpdateAt = tool.UpdateAt
        };
    }

    public async Task<bool> DeleteToolAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == id, ct);
        if (tool is null)
        {
            return false;
        }

        db.Tools.Remove(tool);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IList<RecipeSummaryResponse>> GetRecipesByToolAsync(Guid groupId, Guid toolId,
        CancellationToken ct = default)
    {
        var tool = await db.Tools.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.Id == toolId, ct);

        if (tool is null)
        {
            return [];
        }

        return tool.Recipes.Select(MapRecipeSummary).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RecipeSummaryResponse MapRecipeSummary(Recipe r)
    {
        return new RecipeSummaryResponse
        {
            Id = r.Id,
            Name = r.Name,
            Slug = r.Slug,
            Description = r.Description,
            Image = r.Image,
            OrgUrl = r.OrgUrl,
            Rating = r.Rating,
            GroupId = r.GroupId,
            HouseholdId = r.HouseholdId,
            CreatedAt = r.CreatedAt,
            UpdateAt = r.UpdateAt,
            Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                .ToList(),
            Categories = r.Categories
                .Select(c => new OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug })
                .ToList()
        };
    }

    private async Task<string> EnsureUniqueTagSlugAsync(string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Tags.IgnoreQueryFilters().AnyAsync(t => t.GroupId == groupId && t.Slug == candidate, ct))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }

    private async Task<string> EnsureUniqueCategorySlugAsync(string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Categories.IgnoreQueryFilters().AnyAsync(c => c.GroupId == groupId && c.Slug == candidate, ct))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }

    private async Task<string> EnsureUniqueToolSlugAsync(string slug, Guid groupId, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Tools.IgnoreQueryFilters().AnyAsync(t => t.GroupId == groupId && t.Slug == candidate, ct))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }
}
