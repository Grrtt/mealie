using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;
using Mealie.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController(
    IRecipeService recipeService,
    IRecipeScraperService scraperService,
    IRecipeExportService exportService,
    IRecipeImportService importService,
    ITenantContext tenantContext,
    ApplicationDbContext db) : ControllerBase
{
    // ── CRUD Endpoints (T057-T061) ──────────────────────────────────────────

    [HttpGet]
    [OutputCache(PolicyName = Mealie.Api.Caching.RecipeListCachePolicy.Name)]
    public async Task<ActionResult<PaginatedResponse<RecipeSummaryResponse>>> GetRecipes(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search,
        [FromQuery] string[]? tags, [FromQuery] string[]? categories,
        CancellationToken ct)
    {
        var filter = new RecipeFilter { Search = search, Tags = tags, Categories = categories };
        var result = await recipeService.GetPaginatedAsync(tenantContext.HouseholdId, pagination, filter, ct);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<RecipeSuggestionsResponse>> GetSuggestions(
        [FromQuery] int limit = 20,
        [FromQuery] string? queryFilter = null,
        [FromQuery] int maxMissingFoods = 20,
        [FromQuery] int maxMissingTools = 20,
        [FromQuery] bool includeFoodsOnHand = false,
        [FromQuery] bool includeToolsOnHand = false,
        [FromQuery] Guid[]? foods = null,
        [FromQuery] Guid[]? tools = null,
        CancellationToken ct = default)
    {
        var foodIds = foods?.ToList() ?? [];
        var toolIds = tools?.ToList() ?? [];
        var result = await recipeService.GetSuggestionsAsync(
            tenantContext.HouseholdId, tenantContext.GroupId, limit, queryFilter,
            maxMissingFoods, maxMissingTools, includeFoodsOnHand, includeToolsOnHand,
            foodIds, toolIds, ct);
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
        return Ok(new { slug });
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

    [HttpPost("bulk-actions/settings")]
    public async Task<IActionResult> BulkUpdateSettings(
        [FromBody] BulkUpdateSettingsRequest request, CancellationToken ct)
    {
        var count = 0;
        foreach (var slug in request.Recipes)
        {
            var updateReq = new UpdateRecipeRequest { Settings = request.Settings };
            var result = await recipeService.UpdateAsync(tenantContext.GroupId, slug, updateReq, ct);
            if (result is not null) count++;
        }
        return Ok(new { detail = $"Updated settings for {count} recipes" });
    }

    [HttpPut]
    public async Task<IActionResult> BulkUpdateRecipes(
        [FromBody] BulkUpdateRecipesRequest request, CancellationToken ct)
    {
        var count = 0;
        foreach (var slug in request.Recipes)
        {
            var result = await recipeService.UpdateAsync(tenantContext.GroupId, slug, request.Update, ct);
            if (result is not null) count++;
        }
        return Ok(new { detail = $"Updated {count} recipes" });
    }

    [HttpPatch]
    public async Task<IActionResult> BulkPatchRecipes(
        [FromBody] BulkUpdateRecipesRequest request, CancellationToken ct)
    {
        var count = 0;
        foreach (var slug in request.Recipes)
        {
            var result = await recipeService.UpdateAsync(tenantContext.GroupId, slug, request.Update, ct);
            if (result is not null) count++;
        }
        return Ok(new { detail = $"Updated {count} recipes" });
    }

    [HttpDelete("bulk-actions/export/purge")]
    public async Task<IActionResult> PurgePendingExports(CancellationToken ct)
    {
        var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "exports");
        if (Directory.Exists(exportDir))
        {
            var files = Directory.GetFiles(exportDir);
            int deleted = 0;
            foreach (var file in files)
            {
                try
                {
                    System.IO.File.Delete(file);
                    deleted++;
                }
                catch { /* ignore errors */ }
            }
            return Ok(new { detail = $"Purged {deleted} export files" });
        }
        return Ok(new { detail = "No exports to purge" });
    }

    [HttpGet("bulk-actions/export")]
    public async Task<IActionResult> GetPendingExports(CancellationToken ct)
    {
        var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "exports");
        var files = new List<ExportFileInfo>();
        if (Directory.Exists(exportDir))
        {
            foreach (var file in Directory.GetFiles(exportDir))
            {
                var info = new System.IO.FileInfo(file);
                files.Add(new ExportFileInfo
                {
                    FileName = info.Name,
                    Size = info.Length,
                    CreatedAt = info.CreationTimeUtc
                });
            }
        }
        return Ok(files);
    }

    // ── Recipe Image Endpoints ──────────────────────────────────────────────

    [HttpPost("{slug}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadRecipeImage(
        string slug, IFormFile image, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return NotFound(new { detail = "Recipe not found" });

        if (image is null || image.Length == 0)
            return BadRequest(new { detail = "No image provided" });

        var recipeDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "recipes", slug);
        var imageDir = Path.Combine(recipeDir, "images");
        Directory.CreateDirectory(imageDir);

        var imagePath = Path.Combine(imageDir, "original.webp");

        await using var stream = image.OpenReadStream();
        await using var file = System.IO.File.Create(imagePath);
        await stream.CopyToAsync(file, ct);

        recipe.Image = $"/api/media/recipes/{slug}/images/original.webp";
        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { detail = "Image uploaded", image = recipe.Image });
    }

    [HttpPut("{slug}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReplaceRecipeImage(
        string slug, IFormFile image, CancellationToken ct)
    {
        return await UploadRecipeImage(slug, image, ct);
    }

    [HttpDelete("{slug}/image")]
    public async Task<IActionResult> DeleteRecipeImage(string slug, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return NotFound(new { detail = "Recipe not found" });

        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "recipes", slug, "images", "original.webp");
        if (System.IO.File.Exists(imagePath))
        {
            System.IO.File.Delete(imagePath);
        }

        recipe.Image = null;
        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { detail = "Image deleted" });
    }

    // ── Scraper Endpoints ───────────────────────────────────────────────────

    /// <summary>Test scrape a URL without saving the recipe.</summary>
    [HttpGet("test-scrape-url")]
    public async Task<ActionResult<object>> TestScrapeUrl(
        [FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { detail = "URL is required" });

        var scraped = await scraperService.ScrapeAsync(url, ct);
        if (scraped.ScrapingNotSupported)
            return BadRequest(new { detail = "Could not scrape recipe from the provided URL" });

        var preview = new
        {
            name = scraped.Name,
            description = scraped.Description,
            recipeYield = scraped.RecipeYield,
            totalTime = scraped.TotalTime,
            recipeIngredients = scraped.RecipeIngredient,
            recipeInstructions = scraped.RecipeInstructions,
            keywords = scraped.Keywords,
            categories = scraped.Categories
        };

        return Ok(preview);
    }

    /// <summary>Generate a unique recipe slug.</summary>
    [HttpGet("create")]
    public async Task<ActionResult<SlugResponse>> GenerateSlug(CancellationToken ct)
    {
        var baseSlug = SlugHelper.Generate($"recipe-{Guid.NewGuid().ToString().Substring(0, 8)}");
        var uniqueSlug = await EnsureUniqueSlugAsync(baseSlug, ct);
        return Ok(new SlugResponse { Slug = uniqueSlug });
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }

    /// <summary>Get recipes by category slug.</summary>
    [HttpGet("category")]
    public async Task<ActionResult<PaginatedResponse<RecipeSummaryResponse>>> GetRecipesByCategory(
        [FromQuery] string category,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
            return BadRequest(new { detail = "Category slug is required" });

        var filter = new RecipeFilter { Categories = [category] };
        var result = await recipeService.GetPaginatedAsync(tenantContext.HouseholdId, pagination, filter, ct);
        return Ok(result);
    }

    /// <summary>Get when a recipe was last made.</summary>
    [HttpGet("{slug}/last-made")]
    public async Task<ActionResult<LastMadeResponse>> GetLastMade(string slug, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .Select(r => new { r.LastMade })
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return NotFound(new { detail = "Recipe not found" });

        return Ok(new LastMadeResponse { Timestamp = recipe.LastMade });
    }

    /// <summary>Update when a recipe was last made.</summary>
    [HttpPatch("{slug}/last-made")]
    public async Task<IActionResult> UpdateLastMade(
        string slug, [FromBody] UpdateLastMadeRequest request, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return NotFound(new { detail = "Recipe not found" });

        recipe.LastMade = request.Timestamp;
        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { detail = "Last made timestamp updated" });
    }

    /// <summary>Streaming SSE endpoint: scrape a recipe from a URL.</summary>
    [HttpPost("create/url/stream")]
    public async Task CreateFromUrlStream([FromBody] RecipeScraperRequest request, CancellationToken ct)
    {
        await StreamSseAsync(async onProgress =>
        {
            await onProgress("Fetching recipe...");
            var scraped = await scraperService.ScrapeAsync(request.Url, ct);
            if (scraped.ScrapingNotSupported)
                throw new InvalidOperationException("Could not scrape recipe from the provided URL");

            if (!request.IncludeTags) scraped.Keywords.Clear();
            if (!request.IncludeCategories) scraped.Categories.Clear();

            await onProgress("Saving recipe...");
            var recipe = await recipeService.CreateFromScrapedAsync(
                scraped, tenantContext.HouseholdId, tenantContext.GroupId, ct: ct);
            return recipe?.Slug;
        }, ct);
    }

    /// <summary>Streaming SSE endpoint: scrape a recipe from raw HTML or JSON-LD.</summary>
    [HttpPost("create/html-or-json/stream")]
    public async Task CreateFromHtmlOrJsonStream([FromBody] ScrapeFromHtmlRequest request, CancellationToken ct)
    {
        await StreamSseAsync(async onProgress =>
        {
            await onProgress("Parsing recipe data...");
            var scraped = await scraperService.ScrapeFromHtmlAsync(request.Data, request.Url, ct);
            if (scraped.ScrapingNotSupported)
                throw new InvalidOperationException("Could not parse recipe from the provided data");

            if (!request.IncludeTags) scraped.Keywords.Clear();
            if (!request.IncludeCategories) scraped.Categories.Clear();

            await onProgress("Saving recipe...");
            var recipe = await recipeService.CreateFromScrapedAsync(
                scraped, tenantContext.HouseholdId, tenantContext.GroupId, ct: ct);
            return recipe?.Slug;
        }, ct);
    }

    /// <summary>Non-streaming URL scrape (legacy / simple clients).</summary>
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

    [HttpPost("create/url/bulk")]
    [HttpPost("create-url/bulk")] // legacy alias
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
                if (!request.IncludeTags) scraped.Keywords.Clear();
                if (!request.IncludeCategories) scraped.Categories.Clear();
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

    [HttpPost("create/zip")]
    [HttpPost("create-zip")] // legacy alias
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

    // ── SSE helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Writes an SSE response. <paramref name="work"/> receives an onProgress callback and
    /// returns the recipe slug on success (or null to emit an error event).
    /// </summary>
    private async Task StreamSseAsync(Func<Func<string, Task>, Task<string?>> work, CancellationToken ct)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task SendEvent(string eventName, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var slug = await work(msg => SendEvent("progress", new { message = msg }));
            if (slug is null)
                await SendEvent("error", new { message = "Failed to create recipe" });
            else
                await SendEvent("done", new { slug });
        }
        catch (Exception ex)
        {
            try { await SendEvent("error", new { message = ex.Message }); } catch { /* client disconnected */ }
        }
    }
}


