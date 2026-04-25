using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Parser;

[ApiController]
[Route("api/openai")]
public class OpenAiController(IOptions<AppSettings> settings) : ControllerBase
{
    [HttpPost("parse-ingredient")]
    public IActionResult ParseIngredient([FromBody] object request)
    {
        if (string.IsNullOrEmpty(settings.Value.OpenAiApiKey))
            return StatusCode(424, new { detail = "OpenAI is not configured" });
        return Ok(new { detail = "Not implemented" });
    }

    [HttpPost("parse-recipe")]
    public IActionResult ParseRecipe([FromBody] object request)
    {
        if (string.IsNullOrEmpty(settings.Value.OpenAiApiKey))
            return StatusCode(424, new { detail = "OpenAI is not configured" });
        return Ok(new { detail = "Not implemented" });
    }
}
