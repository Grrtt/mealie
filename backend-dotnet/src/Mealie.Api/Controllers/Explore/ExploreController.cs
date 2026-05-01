using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Explore;

[ApiController]
[Route("api/explore")]
[AllowAnonymous]
public class ExploreController(ApplicationDbContext db) : ControllerBase
{
    // ── Group ────────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}")]
    public async Task<IActionResult> GetGroup(string groupSlug, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        return Ok(new { id = group.Id, name = group.Name, slug = group.Slug });
    }

    // ── Households ──────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/households")]
    public async Task<IActionResult> GetHouseholds(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Households.IgnoreQueryFilters()
            .Include(h => h.Preferences)
            .Where(h => h.GroupId == group.Id && (h.Preferences == null || !h.Preferences.PrivateHousehold));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(h => h.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(h => h.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(h => new HouseholdPublicResponse
            {
                Id = h.Id,
                Name = h.Name,
                Slug = h.Slug,
                RecipeCount = db.Recipes.IgnoreQueryFilters()
                    .Count(r => r.HouseholdId == h.Id && r.Settings != null && r.Settings.Public)
            })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<HouseholdPublicResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items
        });
    }

    [HttpGet("groups/{groupSlug}/households/{householdSlug}")]
    public async Task<IActionResult> GetHousehold(string groupSlug, string householdSlug, CancellationToken ct)
    {
        var household = await ResolvePublicHousehold(groupSlug, householdSlug, ct);
        if (household is null) return NotFound();

        var recipeCount = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == household.Id && r.Settings != null && r.Settings.Public)
            .CountAsync(ct);

        return Ok(new HouseholdPublicResponse
        {
            Id = household.Id,
            Name = household.Name,
            Slug = household.Slug,
            RecipeCount = recipeCount
        });
    }

    // ── Household Recipes ────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/households/{householdSlug}/recipes")]
    public async Task<IActionResult> GetHouseholdRecipes(string groupSlug, string householdSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var household = await ResolvePublicHousehold(groupSlug, householdSlug, ct);
        if (household is null) return NotFound();

        var query = db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == household.Id && r.Settings != null && r.Settings.Public);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(r => r.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(r => new { r.Id, r.Name, r.Slug, r.Image, r.Rating, r.Description })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/households/{householdSlug}/recipes/{recipeSlug}")]
    public async Task<IActionResult> GetHouseholdRecipe(string groupSlug, string householdSlug,
        string recipeSlug, CancellationToken ct)
    {
        var household = await ResolvePublicHousehold(groupSlug, householdSlug, ct);
        if (household is null) return NotFound();

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == household.Id && r.Slug == recipeSlug
                     && r.Settings != null && r.Settings.Public)
            .FirstOrDefaultAsync(ct);

        return recipe is null ? NotFound() : Ok(recipe);
    }

    // ── Group-level Recipes (convenience, all public recipes in group) ────────

    [HttpGet("groups/{groupSlug}/recipes")]
    public async Task<IActionResult> GetGroupRecipes(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Household).ThenInclude(h => h!.Preferences)
            .Where(r => r.GroupId == group.Id && r.Settings != null && r.Settings.Public
                     && (r.Household == null || r.Household.Preferences == null
                         || !r.Household.Preferences.PrivateHousehold));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(r => r.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(r => new { r.Id, r.Name, r.Slug, r.Image, r.Rating, r.Description })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/recipes/{recipeSlug}")]
    public async Task<IActionResult> GetGroupRecipe(string groupSlug, string recipeSlug, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Household).ThenInclude(h => h!.Preferences)
            .Where(r => r.GroupId == group.Id && r.Slug == recipeSlug
                     && r.Settings != null && r.Settings.Public
                     && (r.Household == null || r.Household.Preferences == null
                         || !r.Household.Preferences.PrivateHousehold))
            .FirstOrDefaultAsync(ct);

        return recipe is null ? NotFound() : Ok(recipe);
    }

    // ── Household Cookbooks ──────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/households/{householdSlug}/cookbooks")]
    public async Task<IActionResult> GetHouseholdCookbooks(string groupSlug, string householdSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var household = await ResolvePublicHousehold(groupSlug, householdSlug, ct);
        if (household is null) return NotFound();

        var query = db.Cookbooks.IgnoreQueryFilters()
            .Where(c => c.HouseholdId == household.Id && c.Public);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(c => new { c.Id, c.Name, c.Description, c.Image })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/households/{householdSlug}/cookbooks/{cookbookId:guid}")]
    public async Task<IActionResult> GetHouseholdCookbook(string groupSlug, string householdSlug,
        Guid cookbookId, CancellationToken ct)
    {
        var household = await ResolvePublicHousehold(groupSlug, householdSlug, ct);
        if (household is null) return NotFound();

        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .Include(c => c.Categories)
            .Include(c => c.Tags)
            .Include(c => c.Tools)
            .FirstOrDefaultAsync(c => c.Id == cookbookId && c.HouseholdId == household.Id && c.Public, ct);
        if (cookbook is null) return NotFound();

        var recipes = await BuildCookbookRecipeQuery(cookbook, household.Id)
            .Select(r => new { r.Id, r.Name, r.Slug, r.Image, r.Rating })
            .ToListAsync(ct);

        return Ok(new
        {
            cookbook.Id, cookbook.Name, cookbook.Description, cookbook.Image,
            Recipes = recipes
        });
    }

    // ── Group-level Cookbooks (convenience) ──────────────────────────────────

    [HttpGet("groups/{groupSlug}/cookbooks")]
    public async Task<IActionResult> GetGroupCookbooks(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Cookbooks.IgnoreQueryFilters()
            .Include(c => c.Household).ThenInclude(h => h.Preferences)
            .Where(c => c.GroupId == group.Id && c.Public
                     && (c.Household.Preferences == null || !c.Household.Preferences.PrivateHousehold));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(c => new { c.Id, c.Name, c.Description, c.Image })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/cookbooks/{cookbookId:guid}")]
    public async Task<IActionResult> GetGroupCookbook(string groupSlug, Guid cookbookId, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .Include(c => c.Household).ThenInclude(h => h.Preferences)
            .Include(c => c.Categories)
            .Include(c => c.Tags)
            .Include(c => c.Tools)
            .FirstOrDefaultAsync(c => c.Id == cookbookId && c.GroupId == group.Id && c.Public
                && (c.Household.Preferences == null || !c.Household.Preferences.PrivateHousehold), ct);
        if (cookbook is null) return NotFound();

        var recipes = await BuildCookbookRecipeQuery(cookbook, cookbook.HouseholdId)
            .Select(r => new { r.Id, r.Name, r.Slug, r.Image, r.Rating })
            .ToListAsync(ct);

        return Ok(new
        {
            cookbook.Id, cookbook.Name, cookbook.Description, cookbook.Image,
            Recipes = recipes
        });
    }

    // ── Foods ────────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/foods")]
    public async Task<IActionResult> GetFoods(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Foods.IgnoreQueryFilters().Where(f => f.GroupId == group.Id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(f => new { f.Id, f.Name, f.PluralName })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/foods/{foodId:guid}")]
    public async Task<IActionResult> GetFood(string groupSlug, Guid foodId, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var food = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == foodId && f.GroupId == group.Id, ct);
        return food is null ? NotFound() : Ok(new { food.Id, food.Name, food.PluralName, food.Description });
    }

    // ── Tags ─────────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/tags")]
    [HttpGet("groups/{groupSlug}/organizers/tags")]
    public async Task<IActionResult> GetTags(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Tags.IgnoreQueryFilters().Where(t => t.GroupId == group.Id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/tags/{tagId:guid}")]
    [HttpGet("groups/{groupSlug}/organizers/tags/{tagId:guid}")]
    public async Task<IActionResult> GetTag(string groupSlug, Guid tagId, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var tag = await db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tagId && t.GroupId == group.Id, ct);
        return tag is null ? NotFound() : Ok(new { tag.Id, tag.Name, tag.Slug });
    }

    // ── Categories ───────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/categories")]
    [HttpGet("groups/{groupSlug}/organizers/categories")]
    public async Task<IActionResult> GetCategories(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Categories.IgnoreQueryFilters().Where(c => c.GroupId == group.Id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(c => new { c.Id, c.Name, c.Slug })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/categories/{categoryId:guid}")]
    [HttpGet("groups/{groupSlug}/organizers/categories/{categoryId:guid}")]
    public async Task<IActionResult> GetCategory(string groupSlug, Guid categoryId, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var category = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.GroupId == group.Id, ct);
        return category is null ? NotFound() : Ok(new { category.Id, category.Name, category.Slug });
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/tools")]
    [HttpGet("groups/{groupSlug}/organizers/tools")]
    public async Task<IActionResult> GetTools(string groupSlug,
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var query = db.Tools.IgnoreQueryFilters().Where(t => t.GroupId == group.Id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<object>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Cast<object>().ToList()
        });
    }

    [HttpGet("groups/{groupSlug}/tools/{toolId:guid}")]
    [HttpGet("groups/{groupSlug}/organizers/tools/{toolId:guid}")]
    public async Task<IActionResult> GetTool(string groupSlug, Guid toolId, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return NotFound();

        var tool = await db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == toolId && t.GroupId == group.Id, ct);
        return tool is null ? NotFound() : Ok(new { tool.Id, tool.Name, tool.Slug });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Mealie.Domain.Entities.Core.Household?> ResolvePublicHousehold(
        string groupSlug, string householdSlug, CancellationToken ct)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug, ct);
        if (group is null) return null;

        var household = await db.Households.IgnoreQueryFilters()
            .Include(h => h.Preferences)
            .FirstOrDefaultAsync(h => h.GroupId == group.Id && h.Slug == householdSlug, ct);

        if (household is null) return null;
        if (household.Preferences?.PrivateHousehold == true) return null;
        return household;
    }

    private IQueryable<Mealie.Domain.Entities.Recipes.Recipe> BuildCookbookRecipeQuery(
        Mealie.Domain.Entities.Organizers.Cookbook cookbook, Guid householdId)
    {
        var query = db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Categories)
            .Include(r => r.Tags)
            .Include(r => r.Tools)
            .Where(r => r.HouseholdId == householdId && r.Settings != null && r.Settings.Public);

        if (cookbook.Categories.Count > 0)
        {
            var catIds = cookbook.Categories.Select(c => c.Id).ToList();
            if (cookbook.RequireAllCategories)
                query = query.Where(r => catIds.All(cid => r.Categories.Any(c => c.Id == cid)));
            else
                query = query.Where(r => r.Categories.Any(c => catIds.Contains(c.Id)));
        }

        if (cookbook.Tags.Count > 0)
        {
            var tagIds = cookbook.Tags.Select(t => t.Id).ToList();
            query = query.Where(r => r.Tags.Any(t => tagIds.Contains(t.Id)));
        }

        if (cookbook.Tools.Count > 0)
        {
            var toolIds = cookbook.Tools.Select(t => t.Id).ToList();
            query = query.Where(r => r.Tools.Any(t => toolIds.Contains(t.Id)));
        }

        return query;
    }
}
