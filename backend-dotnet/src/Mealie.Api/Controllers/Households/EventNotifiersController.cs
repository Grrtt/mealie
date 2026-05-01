using Mealie.Application.Commands.Webhooks;
using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Webhooks;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/events/notifications")]
public class EventNotifiersController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        return Ok(await executor.ExecuteAsync(new GetEventNotifiersQuery(CurrentHouseholdId), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventNotifierRequest request,
        CancellationToken ct = default)
    {
        return Ok(await executor.ExecuteAsync(
            new CreateEventNotifierCommand(CurrentGroupId, CurrentHouseholdId, request), ct));
    }

    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid itemId, [FromBody] CreateEventNotifierRequest request,
        CancellationToken ct = default)
    {
        var item = await executor.ExecuteAsync(new UpdateEventNotifierCommand(CurrentHouseholdId, itemId, request), ct);
        if (item is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(item);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId, CancellationToken ct = default)
    {
        var success = await executor.ExecuteAsync(new DeleteEventNotifierCommand(CurrentHouseholdId, itemId), ct);
        if (!success)
        {
            return NotFoundOrForbidden();
        }

        return Ok();
    }

    [HttpPost("{itemId:guid}/test")]
    public async Task<IActionResult> Test(Guid itemId, CancellationToken ct = default)
    {
        await executor.ExecuteAsync(new TestEventNotifierCommand(CurrentHouseholdId, itemId), ct);
        return Ok(new { detail = "Test notification sent" });
    }
}
