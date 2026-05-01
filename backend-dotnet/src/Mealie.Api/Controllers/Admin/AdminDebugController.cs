using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/debug")]
[Authorize(Roles = "admin")]
public class AdminDebugController(IOptions<AppSettings> settings) : ControllerBase
{
    private readonly AppSettings _settings = settings.Value;

    [HttpPost("openai")]
    public async Task<IActionResult> TestOpenAi([FromBody] DebugOpenAiRequest request)
    {
        if (string.IsNullOrEmpty(_settings.OpenAiApiKey))
        {
            return StatusCode(424, new DebugOpenAiResponse
            {
                Success = false,
                Response = "OpenAI API key is not configured"
            });
        }

        try
        {
            var testMessage = request.TestMessage ?? "Hello, can you respond with a simple greeting?";

            // For now, return a stub response indicating OpenAI would be called
            // In a real implementation, this would call the OpenAI API
            return Ok(new DebugOpenAiResponse
            {
                Success = true,
                Response = "OpenAI connection test successful (stub implementation)"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new DebugOpenAiResponse
            {
                Success = false,
                Response = $"Error testing OpenAI: {ex.Message}"
            });
        }
    }
}
