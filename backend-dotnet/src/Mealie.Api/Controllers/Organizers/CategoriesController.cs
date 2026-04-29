using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/categories")]
public class CategoriesController(IOrganizerService organizerService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryResponse>>> GetCategories(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
        => Ok(await organizerService.GetCategoriesAsync(CurrentGroupId, pagination, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(string slug, CancellationToken ct)
    {
        var category = await organizerService.GetCategoryBySlugAsync(CurrentGroupId, slug, ct);
        if (category is null) return NotFoundOrForbidden();
        return Ok(category);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryResponse>> GetCategoryBySlug(string slug, CancellationToken ct)
    {
        var category = await organizerService.GetCategoryBySlugAsync(CurrentGroupId, slug, ct);
        if (category is null) return NotFoundOrForbidden();
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateOrganizerRequest request, CancellationToken ct)
    {
        var category = await organizerService.CreateCategoryAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetCategory), new { slug = category.Slug }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateOrganizerRequest request, CancellationToken ct)
    {
        var category = await organizerService.UpdateCategoryAsync(CurrentGroupId, id, request, ct);
        if (category is null) return NotFoundOrForbidden();
        return Ok(category);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> PatchCategory(Guid id, [FromBody] UpdateOrganizerRequest request, CancellationToken ct)
        => await UpdateCategory(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        var deleted = await organizerService.DeleteCategoryAsync(CurrentGroupId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
