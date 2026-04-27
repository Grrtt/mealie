using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeTimelineController(
    IRecipeTimelineService timelineService,
    ITenantContext tenantContext) : ControllerBase
{
    // GET /api/recipes/timeline/events  (global, group-scoped)
    [HttpGet("timeline/events")]
    public async Task<IActionResult> GetAllEvents(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 32,
        [FromQuery] string? queryFilter = null,
        CancellationToken ct = default)
    {
        var result = await timelineService.GetAllEventsAsync(tenantContext.GroupId, page, perPage, ct);
        return Ok(result);
    }

    [HttpGet("{slug}/timeline")]
    public async Task<ActionResult<IList<TimelineEventResponse>>> GetTimeline(string slug, CancellationToken ct)
    {
        var events = await timelineService.GetEventsAsync(slug, ct);
        return Ok(events);
    }

    [HttpPost("{slug}/timeline")]
    public async Task<ActionResult<TimelineEventResponse>> AddEvent(
        string slug, [FromBody] CreateTimelineEventRequest request, CancellationToken ct)
    {
        var ev = await timelineService.AddEventAsync(slug, tenantContext.UserId, request, ct);
        if (ev is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(ev);
    }

    [HttpPut("{slug}/timeline/{eventId:guid}")]
    public async Task<ActionResult<TimelineEventResponse>> UpdateEvent(
        string slug, Guid eventId, [FromBody] UpdateTimelineEventRequest request, CancellationToken ct)
    {
        var ev = await timelineService.UpdateEventAsync(eventId, request, ct);
        if (ev is null) return NotFound(new { detail = "Timeline event not found" });
        return Ok(ev);
    }

    [HttpDelete("{slug}/timeline/{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(string slug, Guid eventId, CancellationToken ct)
    {
        var deleted = await timelineService.DeleteEventAsync(eventId, ct);
        if (!deleted) return NotFound(new { detail = "Timeline event not found" });
        return NoContent();
    }
}
