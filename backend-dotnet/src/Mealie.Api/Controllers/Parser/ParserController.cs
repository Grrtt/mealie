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
        [FromQuery] string parser = "nlp")
    {
        var result = await parserService.ParseAsync(CurrentGroupId, request.Ingredient, parser);
        return Ok(result);
    }

    [HttpGet("ingredient")]
    public async Task<ActionResult<ParsedIngredientDto>> ParseIngredientGet(
        [FromQuery] string ingredient,
        [FromQuery] string parser = "nlp")
    {
        var result = await parserService.ParseAsync(CurrentGroupId, ingredient, parser);
        return Ok(result);
    }

    [HttpPost("ingredients")]
    public async Task<ActionResult<IList<ParsedIngredientDto>>> ParseIngredients(
        [FromBody] ParseIngredientsRequest request,
        [FromQuery] string parser = "nlp")
    {
        var results = await parserService.ParseBatchAsync(CurrentGroupId, request.Ingredients, parser);
        return Ok(results);
    }
}

public class ParseIngredientRequest
{
    public string Ingredient { get; set; } = string.Empty;
}

public class ParseIngredientsRequest
{
    public IList<string> Ingredients { get; set; } = [];
}
