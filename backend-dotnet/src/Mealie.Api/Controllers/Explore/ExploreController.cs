using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Explore;

[ApiController]
[Route("api/explore")]
[AllowAnonymous]
public class ExploreController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("groups/{groupSlug}")]
    public async Task<IActionResult> GetGroup(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        return Ok(new { id = group.Id, name = group.Name, slug = group.Slug });
    }

    // ── Households ──────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/households")]
    public async Task<IActionResult> GetHouseholds(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var households = await db.Households.IgnoreQueryFilters()
            .Where(h => h.GroupId == group.Id)
            .Select(h => new HouseholdPublicResponse
            {
                Id = h.Id,
                Name = h.Name,
                Slug = h.Slug,
                RecipeCount = h.Group != null
                    ? db.Recipes.IgnoreQueryFilters()
                        .Count(r => r.HouseholdId == h.Id && r.Settings != null && r.Settings.Public)
                    : 0
            })
            .ToListAsync();

        return Ok(households);
    }

    [HttpGet("groups/{groupSlug}/households/{householdSlug}")]
    public async Task<IActionResult> GetHousehold(string groupSlug, string householdSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var household = await db.Households.IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.GroupId == group.Id && h.Slug == householdSlug);
        if (household is null)
        {
            return NotFound();
        }

        var recipeCount = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == household.Id && r.Settings != null && r.Settings.Public)
            .CountAsync();

        return Ok(new HouseholdPublicResponse
        {
            Id = household.Id,
            Name = household.Name,
            Slug = household.Slug,
            RecipeCount = recipeCount
        });
    }

    // ── Recipes ─────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/recipes")]
    public async Task<IActionResult> GetRecipes(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var recipes = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == group.Id && r.Settings != null && r.Settings.Public)
            .Select(r => new { r.Id, r.Name, r.Slug, r.Image, r.Rating })
            .ToListAsync();
        return Ok(recipes);
    }

    [HttpGet("groups/{groupSlug}/recipes/{recipeSlug}")]
    public async Task<IActionResult> GetRecipe(string groupSlug, string recipeSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == group.Id && r.Slug == recipeSlug && r.Settings != null && r.Settings.Public)
            .FirstOrDefaultAsync();
        if (recipe is null)
        {
            return NotFound();
        }

        return Ok(recipe);
    }

    // ── Cookbooks ───────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/cookbooks")]
    public async Task<IActionResult> GetCookbooks(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var cookbooks = await db.Cookbooks.IgnoreQueryFilters()
            .Where(c => c.GroupId == group.Id && c.Public)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync();
        return Ok(cookbooks);
    }

    [HttpGet("groups/{groupSlug}/cookbooks/{cookbookId:guid}")]
    public async Task<IActionResult> GetCookbook(string groupSlug, Guid cookbookId)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .Where(c => c.Id == cookbookId && c.GroupId == group.Id && c.Public)
            .FirstOrDefaultAsync();
        if (cookbook is null)
        {
            return NotFound();
        }

        return Ok(new { cookbook.Id, cookbook.Name, cookbook.Description, cookbook.Image });
    }

    // ── Foods ───────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/foods")]
    public async Task<IActionResult> GetFoods(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var foods = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == group.Id)
            .Select(f => new { f.Id, f.Name })
            .ToListAsync();
        return Ok(foods);
    }

    [HttpGet("groups/{groupSlug}/foods/{foodId:guid}")]
    public async Task<IActionResult> GetFood(string groupSlug, Guid foodId)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var food = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == foodId && f.GroupId == group.Id);
        if (food is null)
        {
            return NotFound();
        }

        return Ok(new { food.Id, food.Name });
    }

    // ── Tags ─────────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/tags")]
    public async Task<IActionResult> GetTags(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var tags = await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == group.Id)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync();
        return Ok(tags);
    }

    [HttpGet("groups/{groupSlug}/tags/{tagId:guid}")]
    public async Task<IActionResult> GetTag(string groupSlug, Guid tagId)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var tag = await db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tagId && t.GroupId == group.Id);
        if (tag is null)
        {
            return NotFound();
        }

        return Ok(new { tag.Id, tag.Name, tag.Slug });
    }

    // ── Categories ──────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/categories")]
    public async Task<IActionResult> GetCategories(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var categories = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == group.Id)
            .Select(c => new { c.Id, c.Name, c.Slug })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("groups/{groupSlug}/categories/{categoryId:guid}")]
    public async Task<IActionResult> GetCategory(string groupSlug, Guid categoryId)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var category = await db.Categories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.GroupId == group.Id);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(new { category.Id, category.Name, category.Slug });
    }

    // ── Tools ───────────────────────────────────────────────────────────────

    [HttpGet("groups/{groupSlug}/tools")]
    public async Task<IActionResult> GetTools(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var tools = await db.Tools.IgnoreQueryFilters()
            .Where(t => t.GroupId == group.Id)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync();
        return Ok(tools);
    }

    [HttpGet("groups/{groupSlug}/tools/{toolId:guid}")]
    public async Task<IActionResult> GetTool(string groupSlug, Guid toolId)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null)
        {
            return NotFound();
        }

        var tool = await db.Tools.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == toolId && t.GroupId == group.Id);
        if (tool is null)
        {
            return NotFound();
        }

        return Ok(new { tool.Id, tool.Name, tool.Slug });
    }

    // ── Organizers (Alternative Paths) ──────────────────────────────────────

    [HttpGet("groups/{groupSlug}/organizers/categories")]
    public async Task<IActionResult> GetOrganizerCategories(string groupSlug)
    {
        return await GetCategories(groupSlug);
    }

    [HttpGet("groups/{groupSlug}/organizers/categories/{categoryId:guid}")]
    public async Task<IActionResult> GetOrganizerCategory(string groupSlug, Guid categoryId)
    {
        return await GetCategory(groupSlug, categoryId);
    }

    [HttpGet("groups/{groupSlug}/organizers/tags")]
    public async Task<IActionResult> GetOrganizerTags(string groupSlug)
    {
        return await GetTags(groupSlug);
    }

    [HttpGet("groups/{groupSlug}/organizers/tags/{tagId:guid}")]
    public async Task<IActionResult> GetOrganizerTag(string groupSlug, Guid tagId)
    {
        return await GetTag(groupSlug, tagId);
    }

    [HttpGet("groups/{groupSlug}/organizers/tools")]
    public async Task<IActionResult> GetOrganizerTools(string groupSlug)
    {
        return await GetTools(groupSlug);
    }

    [HttpGet("groups/{groupSlug}/organizers/tools/{toolId:guid}")]
    public async Task<IActionResult> GetOrganizerTool(string groupSlug, Guid toolId)
    {
        return await GetTool(groupSlug, toolId);
    }
}
