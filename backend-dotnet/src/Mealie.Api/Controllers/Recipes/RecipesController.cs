using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController(
    IRecipeService recipeService,
    IRecipeScraperService scraperService,
    IRecipeExportService exportService,
    IRecipeImportService importService,
    ITenantContext tenantContext) : ControllerBase
{
    // ── CRUD Endpoints (T057-T061) ──────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RecipeSummaryResponse>>> GetRecipes(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search,
        [FromQuery] string[]? tags, [FromQuery] string[]? categories,
        CancellationToken ct)
    {
        var filter = new RecipeFilter { Search = search, Tags = tags, Categories = categories };
        var result = await recipeService.GetPaginatedAsync(tenantContext.HouseholdId, pagination, filter, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RecipeDetailResponse>> CreateRecipe(
        [FromBody] CreateRecipeRequest request, CancellationToken ct)
    {
        var recipe = await recipeService.CreateAsync(
            tenantContext.GroupId, tenantContext.HouseholdId, tenantContext.UserId, request, ct);
        return CreatedAtAction(nameof(GetRecipeBySlug), new { slug = recipe.Slug }, recipe);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetRecipeBySlug(string slug, CancellationToken ct)
    {
        var recipe = await recipeService.GetDetailBySlugAsync(tenantContext.GroupId, slug, ct);
        if (recipe is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(recipe);
    }

    [HttpPut("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> UpdateRecipe(
        string slug, [FromBody] UpdateRecipeRequest request, CancellationToken ct)
    {
        var recipe = await recipeService.UpdateAsync(tenantContext.GroupId, slug, request, ct);
        if (recipe is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(recipe);
    }

    [HttpPatch("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> PatchRecipe(
        string slug, [FromBody] UpdateRecipeRequest request, CancellationToken ct)
    {
        var recipe = await recipeService.UpdateAsync(tenantContext.GroupId, slug, request, ct);
        if (recipe is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(recipe);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteRecipe(string slug, CancellationToken ct)
    {
        var deleted = await recipeService.DeleteAsync(tenantContext.GroupId, slug, ct);
        if (!deleted) return NotFound(new { detail = "Recipe not found" });
        return NoContent();
    }

    [HttpPost("{slug}/duplicate")]
    public async Task<ActionResult<RecipeDetailResponse>> DuplicateRecipe(string slug, CancellationToken ct)
    {
        var recipe = await recipeService.DuplicateAsync(
            tenantContext.GroupId, tenantContext.HouseholdId, tenantContext.UserId, slug, ct);
        if (recipe is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(recipe);
    }

    // ── Bulk Actions (T081) ─────────────────────────────────────────────────

    [HttpPost("bulk-actions/delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkActionRequest request, CancellationToken ct)
    {
        await recipeService.BulkDeleteAsync(request.Recipes, ct);
        return Ok(new { detail = $"Deleted {request.Recipes.Count} recipes" });
    }

    [HttpPost("bulk-actions/tag")]
    public async Task<IActionResult> BulkTag([FromBody] BulkTagRequest request, CancellationToken ct)
    {
        await recipeService.BulkTagAsync(request.Recipes, request.Tags, tenantContext.GroupId, ct);
        return Ok(new { detail = "Tags applied" });
    }

    [HttpPost("bulk-actions/categorize")]
    public async Task<IActionResult> BulkCategorize([FromBody] BulkCategorizeRequest request, CancellationToken ct)
    {
        await recipeService.BulkCategorizeAsync(request.Recipes, request.Categories, tenantContext.GroupId, ct);
        return Ok(new { detail = "Categories applied" });
    }

    [HttpPost("bulk-actions/export")]
    public async Task<IActionResult> BulkExport([FromBody] BulkActionRequest request, CancellationToken ct)
    {
        var results = new List<object>();
        foreach (var slug in request.Recipes)
        {
            var export = await exportService.ExportRecipeAsync(slug, ct);
            if (export is not null)
                results.Add(new { slug, fileName = export.Value.FileName });
        }
        return Ok(new { exported = results.Count, files = results });
    }

    // ── Scraper Endpoints (T086) ────────────────────────────────────────────

    [HttpPost("create-url")]
    public async Task<ActionResult<RecipeSummaryResponse>> CreateFromUrl(
        [FromBody] RecipeScraperRequest request, CancellationToken ct)
    {
        var scraped = await scraperService.ScrapeAsync(request.Url, ct);
        if (scraped.ScrapingNotSupported)
            return BadRequest(new { detail = "Could not scrape recipe from the provided URL" });

        var recipe = await recipeService.CreateFromScrapedAsync(
            scraped, tenantContext.HouseholdId, tenantContext.GroupId, ct: ct);
        if (recipe is null) return BadRequest(new { detail = "Failed to create recipe" });

        return Ok(recipe);
    }

    [HttpPost("create-url/bulk")]
    public async Task<IActionResult> CreateFromUrls([FromBody] BulkScrapeRequest request, CancellationToken ct)
    {
        var results = new List<object>();
        foreach (var url in request.Urls)
        {
            try
            {
                var scraped = await scraperService.ScrapeAsync(url, ct);
                if (scraped.ScrapingNotSupported)
                {
                    results.Add(new { url, success = false, detail = "Scraping not supported" });
                    continue;
                }
                var recipe = await recipeService.CreateFromScrapedAsync(
                    scraped, tenantContext.HouseholdId, tenantContext.GroupId, ct: ct);
                results.Add(new { url, success = recipe is not null, slug = recipe?.Slug });
            }
            catch (Exception ex)
            {
                results.Add(new { url, success = false, detail = ex.Message });
            }
        }
        return Ok(results);
    }

    // ── Export Endpoints (T087) ─────────────────────────────────────────────

    [HttpGet("exports")]
    public async Task<IActionResult> GetExports(CancellationToken ct)
    {
        var exports = await exportService.GetExportsAsync(ct);
        return Ok(exports);
    }

    [HttpGet("{slug}/exports")]
    public async Task<IActionResult> ExportRecipe(string slug, CancellationToken ct)
    {
        var result = await exportService.ExportRecipeAsync(slug, ct);
        if (result is null) return NotFound(new { detail = "Recipe not found" });
        return File(result.Value.Data, "application/zip", result.Value.FileName);
    }

    // ── Import Endpoints (T088) ─────────────────────────────────────────────

    [HttpPost("create-zip")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFromZip(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "No file provided" });

        await using var stream = file.OpenReadStream();
        var count = await importService.ImportFromZipAsync(
            stream, tenantContext.HouseholdId, tenantContext.GroupId, ct);
        return Ok(new { imported = count });
    }

    [HttpPost("create-image-ocr")]
    public IActionResult CreateFromImageOcr()
        => StatusCode(501, new { detail = "OCR import is not implemented" });
}
