using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Parser;

[ApiController]
[Route("api/openai")]
public class OpenAiController : ControllerBase
{
    [HttpPost("parse-ingredient")]
    public IActionResult ParseIngredient([FromBody] object request) =>
        StatusCode(410, new { detail = "This endpoint is removed. Use /api/parser/ingredient instead." });

    [HttpPost("parse-recipe")]
    public IActionResult ParseRecipe([FromBody] object request) =>
        StatusCode(410, new { detail = "This endpoint is removed. AI parsing is configured via Admin > AI Configurations." });
}
