using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Mealie.Application.Commands.Recipes;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
public class RecipeShareController(
    QueryExecutor executor,
    ITenantContext tenantContext) : ControllerBase
{
    [Authorize]
    [HttpGet("{slug}/share")]
    public async Task<ActionResult<IList<ShareTokenResponse>>> GetShareTokens(string slug, CancellationToken ct)
    {
        var tokens = await executor.ExecuteAsync(new GetShareTokensQuery(slug), ct);
        return Ok(tokens);
    }

    [Authorize]
    [HttpPost("{slug}/share")]
    public async Task<ActionResult<ShareTokenResponse>> CreateShareToken(
        string slug, [FromBody] CreateShareTokenRequest request, CancellationToken ct)
    {
        var token = await executor.ExecuteAsync(new CreateShareTokenCommand(slug, tenantContext.GroupId, request), ct);
        if (token is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(token);
    }

    [Authorize]
    [HttpDelete("{slug}/share/{tokenId:guid}")]
    public async Task<IActionResult> DeleteShareToken(string slug, Guid tokenId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteShareTokenCommand(tokenId), ct);
        if (!deleted) return NotFound(new { detail = "Share token not found" });
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("shared/{tokenId:guid}")]
    public async Task<ActionResult<ShareTokenResponse>> GetSharedRecipe(Guid tokenId, CancellationToken ct)
    {
        var token = await executor.ExecuteAsync(new GetShareTokenQuery(tokenId), ct);
        if (token is null) return NotFound(new { detail = "Share token not found or expired" });
        return Ok(token);
    }
}
