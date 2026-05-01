using System.Text.Json;
using Mealie.Api.Caching;
using Mealie.Application.Commands.Recipes;
using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Mealie.Application.Services.Images;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController(
    QueryExecutor executor,
    IRecipeScraperService scraperService,
    IRecipeImportService importService,
    ITenantContext tenantContext,
    IOptions<AppSettings> appSettings,
    ApplicationDbContext db) : ControllerBase
{
    // ── CRUD Endpoints ──────────────────────────────────────────────────────

    [HttpGet]
    [OutputCache(PolicyName = RecipeListCachePolicy.Name)]
    public async Task<ActionResult<PaginatedResponse<RecipeSummaryResponse>>> GetRecipes(
        [FromQuery] PaginationParams pagination,
        [FromQuery] string? search,
        [FromQuery] string[]? tags,
        [FromQuery] string[]? categories,
        [FromQuery] string[]? foods,
        [FromQuery] string[]? tools,
        [FromQuery] string[]? households,
        [FromQuery] bool requireAllCategories = false,
        [FromQuery] bool requireAllTags = false,
        [FromQuery] bool requireAllTools = false,
        [FromQuery] bool requireAllFoods = false,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? orderDirection = null,
        [FromQuery] string? queryFilter = null,
        CancellationToken ct = default)
    {
        var filter = new RecipeFilter
        {
            Search = search,
            Tags = tags,
            Categories = categories,
            Foods = foods,
            Tools = tools,
            Households = households,
            RequireAllCategories = requireAllCategories,
            RequireAllTags = requireAllTags,
            RequireAllTools = requireAllTools,
            RequireAllFoods = requireAllFoods,
            OrderBy = orderBy,
            OrderDirection = orderDirection,
            QueryFilter = queryFilter
        };
        var result = await executor.ExecuteAsync(
            new GetPaginatedRecipesQuery(tenantContext.HouseholdId, pagination, filter), ct);
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
        var result = await executor.ExecuteAsync(new GetRecipeSuggestionsQuery(
            tenantContext.HouseholdId, tenantContext.GroupId, limit, queryFilter,
            maxMissingFoods, maxMissingTools, includeFoodsOnHand, includeToolsOnHand,
            foodIds, toolIds), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RecipeDetailResponse>> CreateRecipe(
        [FromBody] CreateRecipeRequest request, CancellationToken ct)
    {
        var recipe = await executor.ExecuteAsync(new CreateRecipeCommand(
            tenantContext.GroupId, tenantContext.HouseholdId, tenantContext.UserId, request), ct);
        return CreatedAtAction(nameof(GetRecipeBySlug), new { slug = recipe.Slug }, recipe);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetRecipeBySlug(string slug, CancellationToken ct)
    {
        var recipe = await executor.ExecuteAsync(new GetRecipeDetailBySlugQuery(tenantContext.GroupId, slug), ct);
        if (recipe is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return Ok(recipe);
    }

    [HttpPut("{slug}")]
    [HttpPatch("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> UpdateRecipe(
        string slug, [FromBody] UpdateRecipeRequest request, CancellationToken ct)
    {
        var recipe = await executor.ExecuteAsync(new UpdateRecipeCommand(tenantContext.GroupId, slug, request), ct);
        if (recipe is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return Ok(recipe);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteRecipe(string slug, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteRecipeCommand(tenantContext.GroupId, slug), ct);
        if (!deleted)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return Ok(new { slug });
    }

    [HttpPost("{slug}/duplicate")]
    public async Task<ActionResult<RecipeDetailResponse>> DuplicateRecipe(string slug, CancellationToken ct)
    {
        var recipe = await executor.ExecuteAsync(new DuplicateRecipeCommand(
            tenantContext.GroupId, tenantContext.HouseholdId, tenantContext.UserId, slug), ct);
        if (recipe is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return Ok(recipe);
    }

    // ── Bulk Actions ────────────────────────────────────────────────────────

    [HttpPost("bulk-actions/delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkActionRequest request, CancellationToken ct)
    {
        await executor.ExecuteAsync(new BulkDeleteRecipesCommand(request.Recipes), ct);
        return Ok(new { detail = $"Deleted {request.Recipes.Count} recipes" });
    }

    [HttpPost("bulk-actions/tag")]
    public async Task<IActionResult> BulkTag([FromBody] BulkTagRequest request, CancellationToken ct)
    {
        await executor.ExecuteAsync(new BulkTagRecipesCommand(request.Recipes, request.Tags, tenantContext.GroupId),
            ct);
        return Ok(new { detail = "Tags applied" });
    }

    [HttpPost("bulk-actions/categorize")]
    public async Task<IActionResult> BulkCategorize([FromBody] BulkCategorizeRequest request, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            new BulkCategorizeRecipesCommand(request.Recipes, request.Categories, tenantContext.GroupId), ct);
        return Ok(new { detail = "Categories applied" });
    }

    [HttpPost("bulk-actions/export")]
    public async Task<IActionResult> BulkExport([FromBody] BulkActionRequest request, CancellationToken ct)
    {
        var export = await executor.ExecuteAsync(
            new BulkExportCommand(request.Recipes, tenantContext.GroupId), ct);
        if (export is null)
        {
            return NotFound(new { detail = "No matching recipes found" });
        }

        return Ok(new { exported = request.Recipes.Count, file = export });
    }

    [HttpPost("bulk-actions/settings")]
    public async Task<IActionResult> BulkUpdateSettings(
        [FromBody] BulkUpdateSettingsRequest request, CancellationToken ct)
    {
        var count = 0;
        foreach (var slug in request.Recipes)
        {
            var updateReq = new UpdateRecipeRequest { Settings = request.Settings };
            var result = await executor.ExecuteAsync(
                new UpdateRecipeCommand(tenantContext.GroupId, slug, updateReq), ct);
            if (result is not null)
            {
                count++;
            }
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
            var result = await executor.ExecuteAsync(
                new UpdateRecipeCommand(tenantContext.GroupId, slug, request.Update), ct);
            if (result is not null)
            {
                count++;
            }
        }

        return Ok(new { detail = $"Updated {count} recipes" });
    }

    [HttpPatch]
    public async Task<IActionResult> BulkPatchRecipes(
        [FromBody] BulkUpdateRecipesRequest request, CancellationToken ct)
    {
        return await BulkUpdateRecipes(request, ct);
    }

    [HttpDelete("bulk-actions/export/purge")]
    public async Task<IActionResult> PurgePendingExports(CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new PurgeExportsCommand(), ct);
        return Ok(new { detail = $"Purged {deleted} export files" });
    }

    [HttpGet("bulk-actions/export")]
    public async Task<IActionResult> GetPendingExports(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetPendingExportsQuery(), ct));
    }

    [HttpGet("bulk-actions/export/{fileName}")]
    public async Task<IActionResult> DownloadExport(string fileName, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new DownloadExportQuery(fileName), ct);
        if (result is null)
        {
            return NotFound(new { detail = "Export file not found" });
        }

        return File(result.Value.Stream, "application/zip", result.Value.FileName);
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
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        if (image is null || image.Length == 0)
        {
            return BadRequest(new { detail = "No image provided" });
        }

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var imageBytes = ms.ToArray();

        var imageDir = Path.Combine(appSettings.Value.DataDir, "recipes", recipe.Id.ToString(), "images");
        RecipeImageProcessor.SaveVariants(imageDir, imageBytes);

        recipe.Image = "original.webp";
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
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        var imageDir = Path.Combine(appSettings.Value.DataDir, "recipes", recipe.Id.ToString(), "images");
        foreach (var variant in new[] { "original.webp", "min-original.webp", "tiny-original.webp" })
        {
            var p = Path.Combine(imageDir, variant);
            if (System.IO.File.Exists(p))
            {
                System.IO.File.Delete(p);
            }
        }

        recipe.Image = null;
        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { detail = "Image deleted" });
    }

    // ── Scraper Endpoints ───────────────────────────────────────────────────

    [HttpGet("test-scrape-url")]
    public async Task<ActionResult<object>> TestScrapeUrl(
        [FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { detail = "URL is required" });
        }

        var scraped = await scraperService.ScrapeAsync(url, ct);
        if (scraped.ScrapingNotSupported)
        {
            return BadRequest(new { detail = "Could not scrape recipe from the provided URL" });
        }

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
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }

    [HttpGet("category")]
    public async Task<ActionResult<PaginatedResponse<RecipeSummaryResponse>>> GetRecipesByCategory(
        [FromQuery] string category,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest(new { detail = "Category slug is required" });
        }

        var filter = new RecipeFilter { Categories = [category] };
        var result = await executor.ExecuteAsync(
            new GetPaginatedRecipesQuery(tenantContext.HouseholdId, pagination, filter), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/last-made")]
    public async Task<ActionResult<LastMadeResponse>> GetLastMade(string slug, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .Select(r => new { r.LastMade })
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return Ok(new LastMadeResponse { Timestamp = recipe.LastMade });
    }

    [HttpPatch("{slug}/last-made")]
    public async Task<IActionResult> UpdateLastMade(
        string slug, [FromBody] UpdateLastMadeRequest request, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Slug == slug && r.GroupId == tenantContext.GroupId)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

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
            {
                throw new InvalidOperationException("Could not scrape recipe from the provided URL");
            }

            if (!request.IncludeTags)
            {
                scraped.Keywords.Clear();
            }

            if (!request.IncludeCategories)
            {
                scraped.Categories.Clear();
            }

            await onProgress("Saving recipe...");
            var recipe = await executor.ExecuteAsync(new CreateRecipeFromScrapedCommand(
                scraped, tenantContext.HouseholdId, tenantContext.GroupId), ct);
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
            {
                throw new InvalidOperationException("Could not parse recipe from the provided data");
            }

            if (!request.IncludeTags)
            {
                scraped.Keywords.Clear();
            }

            if (!request.IncludeCategories)
            {
                scraped.Categories.Clear();
            }

            await onProgress("Saving recipe...");
            var recipe = await executor.ExecuteAsync(new CreateRecipeFromScrapedCommand(
                scraped, tenantContext.HouseholdId, tenantContext.GroupId), ct);
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
        {
            return BadRequest(new { detail = "Could not scrape recipe from the provided URL" });
        }

        var recipe = await executor.ExecuteAsync(new CreateRecipeFromScrapedCommand(
            scraped, tenantContext.HouseholdId, tenantContext.GroupId), ct);
        if (recipe is null)
        {
            return BadRequest(new { detail = "Failed to create recipe" });
        }

        return Ok(recipe);
    }

    [HttpPost("create/url/bulk")]
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

                if (!request.IncludeTags)
                {
                    scraped.Keywords.Clear();
                }

                if (!request.IncludeCategories)
                {
                    scraped.Categories.Clear();
                }

                var recipe = await executor.ExecuteAsync(new CreateRecipeFromScrapedCommand(
                    scraped, tenantContext.HouseholdId, tenantContext.GroupId), ct);
                results.Add(new { url, success = recipe is not null, slug = recipe?.Slug });
            }
            catch (Exception ex)
            {
                results.Add(new { url, success = false, detail = ex.Message });
            }
        }

        return Ok(results);
    }

    // ── Export Endpoints ────────────────────────────────────────────────────

    [HttpGet("exports")]
    public async Task<IActionResult> GetExports(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetExportsQuery(), ct));
    }

    [HttpGet("{slug}/exports")]
    public async Task<IActionResult> ExportRecipe(string slug, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new ExportRecipeQuery(slug), ct);
        if (result is null)
        {
            return NotFound(new { detail = "Recipe not found" });
        }

        return File(result.Value.Data, "application/zip", result.Value.FileName);
    }

    // ── Import Endpoints ────────────────────────────────────────────────────

    [HttpPost("create/zip")]
    [HttpPost("create-zip")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFromZip(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { detail = "No file provided" });
        }

        await using var stream = file.OpenReadStream();
        var count = await importService.ImportFromZipAsync(
            stream, tenantContext.HouseholdId, tenantContext.GroupId, ct);
        return Ok(new { imported = count });
    }

    [HttpPost("create-image-ocr")]
    public IActionResult CreateFromImageOcr()
    {
        return StatusCode(501, new { detail = "OCR import is not implemented" });
    }

    // ── SSE helper ─────────────────────────────────────────────────────────

    private async Task StreamSseAsync(Func<Func<string, Task>, Task<string?>> work, CancellationToken ct)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task SendEvent(string eventName, object data)
        {
            var json = JsonSerializer.Serialize(data,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var slug = await work(msg => SendEvent("progress", new { message = msg }));
            if (slug is null)
            {
                await SendEvent("error", new { message = "Failed to create recipe" });
            }
            else
            {
                await SendEvent("done", new { slug });
            }
        }
        catch (Exception ex)
        {
            try
            {
                await SendEvent("error", new { message = ex.Message });
            }
            catch
            {
                /* client disconnected */
            }
        }
    }
}
