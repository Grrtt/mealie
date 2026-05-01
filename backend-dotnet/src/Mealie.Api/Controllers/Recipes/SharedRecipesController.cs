using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/shared/recipes")]
public class SharedRecipesController(
    QueryExecutor executor,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShareTokenResponse>> GetSharedRecipe(Guid id, CancellationToken ct)
    {
        var token = await executor.ExecuteAsync(new GetShareTokenQuery(id), ct);
        if (token is null) return NotFound(new { detail = "Share token not found or expired" });
        return Ok(token);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ShareTokenResponse>> CreateSharedRecipe(
        [FromBody] CreateSharedRecipeRequest request, CancellationToken ct)
    {
        var token = await executor.ExecuteAsync(new CreateShareTokenCommand(
            request.RecipeSlug, tenantContext.GroupId,
            new CreateShareTokenRequest { ExpiresAt = request.ExpiresAt }), ct);
        if (token is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(token);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ShareTokenResponse>> UpdateSharedRecipe(
        Guid id, [FromBody] CreateShareTokenRequest request, CancellationToken ct)
    {
        var token = await executor.ExecuteAsync(new GetShareTokenQuery(id), ct);
        if (token is null) return NotFound(new { detail = "Share token not found" });
        return Ok(token);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ShareTokenResponse>> PatchSharedRecipe(
        Guid id, [FromBody] CreateShareTokenRequest request, CancellationToken ct)
        => await UpdateSharedRecipe(id, request, ct);

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSharedRecipe(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteShareTokenCommand(id), ct);
        if (!deleted) return NotFound(new { detail = "Share token not found" });
        return NoContent();
    }
}

public class CreateSharedRecipeRequest
{
    public string RecipeSlug { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
