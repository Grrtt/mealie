using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Services.Webhooks;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/self/webhooks")]
public class WebhooksController(IWebhookService webhookService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await webhookService.GetAllAsync(CurrentHouseholdId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request)
    {
        var item = await webhookService.CreateAsync(CurrentGroupId, CurrentHouseholdId, request);
        return Ok(item);
    }

    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid itemId, [FromBody] CreateWebhookRequest request)
    {
        var item = await webhookService.UpdateAsync(CurrentHouseholdId, itemId, request);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId)
    {
        var success = await webhookService.DeleteAsync(CurrentHouseholdId, itemId);
        if (!success) return NotFoundOrForbidden();
        return Ok();
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test([FromBody] TestWebhookRequest request)
    {
        await webhookService.TestAsync(request.Url);
        return Ok(new { detail = "Test webhook delivered" });
    }
}
