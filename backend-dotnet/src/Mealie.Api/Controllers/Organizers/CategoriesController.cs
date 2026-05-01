using Mealie.Application.Commands.Organizers;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/categories")]
public class CategoriesController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryResponse>>> GetCategories(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetCategoriesQuery(CurrentGroupId, pagination, search), ct));
    }

    [HttpGet("empty")]
    public async Task<ActionResult<IList<CategoryResponse>>> GetEmptyCategories(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetEmptyCategoriesQuery(CurrentGroupId), ct));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(string slug, CancellationToken ct)
    {
        var category = await executor.ExecuteAsync(new GetCategoryBySlugQuery(CurrentGroupId, slug), ct);
        if (category is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(category);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryResponse>> GetCategoryBySlug(string slug, CancellationToken ct)
    {
        return await GetCategory(slug, ct);
    }

    [HttpGet("{id:guid}/recipes")]
    public async Task<ActionResult<IList<RecipeSummaryResponse>>> GetCategoryRecipes(Guid id, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetRecipesByCategoryQuery(CurrentGroupId, id), ct));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateOrganizerRequest request,
        CancellationToken ct)
    {
        var category = await executor.ExecuteAsync(new CreateCategoryCommand(CurrentGroupId, request), ct);
        return CreatedAtAction(nameof(GetCategory), new { slug = category.Slug }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateOrganizerRequest request,
        CancellationToken ct)
    {
        var category = await executor.ExecuteAsync(new UpdateCategoryCommand(CurrentGroupId, id, request), ct);
        if (category is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(category);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> PatchCategory(Guid id, [FromBody] UpdateOrganizerRequest request,
        CancellationToken ct)
    {
        return await UpdateCategory(id, request, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteCategoryCommand(CurrentGroupId, id), ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }
}
