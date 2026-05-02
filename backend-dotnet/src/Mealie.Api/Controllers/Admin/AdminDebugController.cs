using Mealie.Application.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/debug")]
[Authorize(Roles = "admin")]
public class AdminDebugController : ControllerBase
{
    [HttpPost("openai")]
    public IActionResult TestOpenAi([FromBody] DebugOpenAiRequest request)
    {
        return StatusCode(410, new DebugOpenAiResponse
        {
            Success = false,
            Response = "This endpoint is removed. Configure AI providers via Admin > AI Configurations."
        });
    }
}
