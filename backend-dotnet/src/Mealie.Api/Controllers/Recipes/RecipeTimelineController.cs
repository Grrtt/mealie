using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Mealie.Application.Commands.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeTimelineController(
    QueryExecutor executor,
    ITenantContext tenantContext,
    IOptions<AppSettings> appSettings) : ControllerBase
{
    [HttpGet("timeline/events")]
    public async Task<IActionResult> GetAllEvents(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 32,
        [FromQuery] string? queryFilter = null,
        CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(new GetAllTimelineEventsQuery(tenantContext.GroupId, page, perPage), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/timeline")]
    public async Task<ActionResult<IList<TimelineEventResponse>>> GetTimeline(string slug, CancellationToken ct)
    {
        var events = await executor.ExecuteAsync(new GetTimelineEventsQuery(slug), ct);
        return Ok(events);
    }

    [HttpPost("{slug}/timeline")]
    public async Task<ActionResult<TimelineEventResponse>> AddEvent(
        string slug, [FromBody] CreateTimelineEventRequest request, CancellationToken ct)
    {
        var ev = await executor.ExecuteAsync(new AddTimelineEventCommand(slug, tenantContext.UserId, request), ct);
        if (ev is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(ev);
    }

    [HttpPut("{slug}/timeline/{eventId:guid}")]
    public async Task<ActionResult<TimelineEventResponse>> UpdateEvent(
        string slug, Guid eventId, [FromBody] UpdateTimelineEventRequest request, CancellationToken ct)
    {
        var ev = await executor.ExecuteAsync(new UpdateTimelineEventCommand(eventId, request), ct);
        if (ev is null) return NotFound(new { detail = "Timeline event not found" });
        return Ok(ev);
    }

    [HttpDelete("{slug}/timeline/{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(string slug, Guid eventId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteTimelineEventCommand(eventId), ct);
        if (!deleted) return NotFound(new { detail = "Timeline event not found" });
        return NoContent();
    }

    [HttpPost("timeline/events")]
    public async Task<ActionResult<TimelineEventResponse>> AddEventFlat(
        [FromBody] CreateTimelineEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RecipeSlug))
            return BadRequest(new { detail = "RecipeSlug is required" });

        var ev = await executor.ExecuteAsync(new AddTimelineEventCommand(request.RecipeSlug, tenantContext.UserId, request), ct);
        if (ev is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(ev);
    }

    [HttpPut("timeline/events/{eventId:guid}")]
    public async Task<ActionResult<TimelineEventResponse>> UpdateEventFlat(
        Guid eventId, [FromBody] UpdateTimelineEventRequest request, CancellationToken ct)
    {
        var ev = await executor.ExecuteAsync(new UpdateTimelineEventCommand(eventId, request), ct);
        if (ev is null) return NotFound(new { detail = "Timeline event not found" });
        return Ok(ev);
    }

    [HttpDelete("timeline/events/{eventId:guid}")]
    public async Task<IActionResult> DeleteEventFlat(Guid eventId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteTimelineEventCommand(eventId), ct);
        if (!deleted) return NotFound(new { detail = "Timeline event not found" });
        return NoContent();
    }

    [HttpPut("timeline/events/{eventId:guid}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTimelineEventImage(
        Guid eventId, IFormFile image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new { detail = "No image provided" });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var result = await executor.ExecuteAsync(new UploadTimelineImageCommand(eventId, ms.ToArray()), ct);
        if (result is null) return NotFound(new { detail = "Timeline event not found" });
        return Ok(result);
    }
}
