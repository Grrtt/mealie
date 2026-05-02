using Mealie.Application.Services.Parser;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Parser;

[ApiController]
[Route("api/parser")]
public class ParserController(IIngredientParserService parserService, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpPost("ingredient")]
    public async Task<ActionResult<ParsedIngredientDto>> ParseIngredient(
        [FromBody] ParseIngredientRequest request,
        CancellationToken ct)
    {
        // Non-admins always use the site default; their parser preference is ignored (FR-006 / SC-002)
        var effectiveParser = tenantContext.IsAdmin ? request.Parser : null;

        var result = await parserService.ParseAsync(CurrentGroupId, request.Ingredient, effectiveParser, ct);
        return Ok(result);
    }

    [HttpGet("ingredient")]
    public async Task<ActionResult<ParsedIngredientDto>> ParseIngredientGet(
        [FromQuery] string ingredient,
        [FromQuery] string? parser,
        CancellationToken ct)
    {
        var effectiveParser = tenantContext.IsAdmin ? parser : null;
        var result = await parserService.ParseAsync(CurrentGroupId, ingredient, effectiveParser, ct);
        return Ok(result);
    }

    [HttpPost("ingredients")]
    public async Task<ActionResult<IList<ParsedIngredientDto>>> ParseIngredients(
        [FromBody] ParseIngredientsRequest request,
        CancellationToken ct)
    {
        var effectiveParser = tenantContext.IsAdmin ? request.Parser : null;
        var results = await parserService.ParseBatchAsync(CurrentGroupId, request.Ingredients, effectiveParser, ct);
        return Ok(results);
    }
}

public class ParseIngredientRequest
{
    public string Ingredient { get; set; } = string.Empty;

    /// <summary>
    ///     "nlp" | "brute" | &lt;ai_configuration_uuid&gt; | null.
    ///     Null or absent means "use the site-wide default".
    ///     For non-admin users this field is always ignored server-side.
    /// </summary>
    public string? Parser { get; set; }
}

public class ParseIngredientsRequest
{
    public IList<string> Ingredients { get; set; } = [];

    /// <inheritdoc cref="ParseIngredientRequest.Parser" />
    public string? Parser { get; set; }
}
