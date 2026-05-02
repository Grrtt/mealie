using Mealie.Application.Commands.Admin;
using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

/// <summary>
///     Admin CRUD + activate endpoints for AI provider configurations.
///     GET  /api/admin/ai-configurations
///     POST /api/admin/ai-configurations
///     GET  /api/admin/ai-configurations/{id}
///     PUT  /api/admin/ai-configurations/{id}
///     DELETE /api/admin/ai-configurations/{id}
///     PUT  /api/admin/ai-configurations/{id}/activate
/// </summary>
[ApiController]
[Route("api/admin/ai-configurations")]
[Authorize(Roles = "admin")]
public class AdminAiConfigurationsController(QueryExecutor executor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new GetAllAiConfigurationsQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AiConfigurationResponse>> Create(
        [FromBody] CreateAiConfigurationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ProviderType))
        {
            return UnprocessableEntity(new { detail = "name and providerType are required." });
        }

        var result = await executor.ExecuteAsync(new CreateAiConfigurationCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AiConfigurationResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new GetAiConfigurationQuery(id), ct);
        return result is null ? NotFound(new { detail = "AI configuration not found." }) : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AiConfigurationResponse>> Update(
        Guid id, [FromBody] UpdateAiConfigurationRequest request, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new UpdateAiConfigurationCommand(id, request), ct);
        return result is null ? NotFound(new { detail = "AI configuration not found." }) : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteAiConfigurationCommand(id), ct);
        return deleted ? NoContent() : NotFound(new { detail = "AI configuration not found." });
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<AiConfigurationResponse>> Activate(Guid id, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new ActivateAiConfigurationCommand(id), ct);
        return result is null ? NotFound(new { detail = "AI configuration not found." }) : Ok(result);
    }
}
