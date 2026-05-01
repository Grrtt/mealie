using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Webhooks;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/webhooks")]
public class WebhooksController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await executor.ExecuteAsync(new GetWebhooksQuery(CurrentHouseholdId), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken ct = default)
    {
        var item = await executor.ExecuteAsync(new GetWebhookByIdQuery(CurrentHouseholdId, id), ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpPost("rerun")]
    public async Task<IActionResult> Rerun(CancellationToken ct = default)
    {
        await executor.ExecuteAsync(new RerunWebhooksForHouseholdCommand(CurrentHouseholdId), ct);
        return Ok(new { detail = "Webhooks rerun for today" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request, CancellationToken ct = default)
        => Ok(await executor.ExecuteAsync(new CreateWebhookCommand(CurrentGroupId, CurrentHouseholdId, request), ct));

    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid itemId, [FromBody] CreateWebhookRequest request, CancellationToken ct = default)
    {
        var item = await executor.ExecuteAsync(new UpdateWebhookCommand(CurrentHouseholdId, itemId, request), ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId, CancellationToken ct = default)
    {
        var success = await executor.ExecuteAsync(new DeleteWebhookCommand(CurrentHouseholdId, itemId), ct);
        if (!success) return NotFoundOrForbidden();
        return Ok();
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, [FromBody] TestWebhookRequest request, CancellationToken ct = default)
    {
        await executor.ExecuteAsync(new TestWebhookCommand(request.Url), ct);
        return Ok(new { detail = "Test webhook delivered" });
    }
}
