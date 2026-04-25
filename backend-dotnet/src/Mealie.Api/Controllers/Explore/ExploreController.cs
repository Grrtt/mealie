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
        if (group is null) return NotFound();
        return Ok(new { id = group.Id, name = group.Name, slug = group.Slug });
    }

    [HttpGet("groups/{groupSlug}/recipes")]
    public async Task<IActionResult> GetRecipes(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();

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
        if (group is null) return NotFound();

        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == group.Id && r.Slug == recipeSlug && r.Settings != null && r.Settings.Public)
            .FirstOrDefaultAsync();
        if (recipe is null) return NotFound();
        return Ok(recipe);
    }

    [HttpGet("groups/{groupSlug}/cookbooks")]
    public async Task<IActionResult> GetCookbooks(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();
        var cookbooks = await db.Cookbooks.IgnoreQueryFilters()
            .Where(c => c.GroupId == group.Id && c.Public)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync();
        return Ok(cookbooks);
    }

    [HttpGet("groups/{groupSlug}/tags")]
    public async Task<IActionResult> GetTags(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();
        var tags = await db.Tags.IgnoreQueryFilters()
            .Where(t => t.GroupId == group.Id)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync();
        return Ok(tags);
    }

    [HttpGet("groups/{groupSlug}/categories")]
    public async Task<IActionResult> GetCategories(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();
        var categories = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.GroupId == group.Id)
            .Select(c => new { c.Id, c.Name, c.Slug })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("groups/{groupSlug}/tools")]
    public async Task<IActionResult> GetTools(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();
        var tools = await db.Tools.IgnoreQueryFilters()
            .Where(t => t.GroupId == group.Id)
            .Select(t => new { t.Id, t.Name, t.Slug })
            .ToListAsync();
        return Ok(tools);
    }

    [HttpGet("groups/{groupSlug}/foods")]
    public async Task<IActionResult> GetFoods(string groupSlug)
    {
        var group = await db.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Slug == groupSlug);
        if (group is null) return NotFound();
        var foods = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == group.Id)
            .Select(f => new { f.Id, f.Name })
            .ToListAsync();
        return Ok(foods);
    }
}
