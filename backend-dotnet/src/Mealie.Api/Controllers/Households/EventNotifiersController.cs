using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Services.Webhooks;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/events/notifications")]
public class EventNotifiersController(IEventNotifierService notifierService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await notifierService.GetAllAsync(CurrentHouseholdId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventNotifierRequest request)
        => Ok(await notifierService.CreateAsync(CurrentGroupId, CurrentHouseholdId, request));

    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid itemId, [FromBody] CreateEventNotifierRequest request)
    {
        var item = await notifierService.UpdateAsync(CurrentHouseholdId, itemId, request);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId)
    {
        var success = await notifierService.DeleteAsync(CurrentHouseholdId, itemId);
        if (!success) return NotFoundOrForbidden();
        return Ok();
    }

    [HttpPost("{itemId:guid}/test")]
    public async Task<IActionResult> Test(Guid itemId)
    {
        await notifierService.TestAsync(CurrentHouseholdId, itemId);
        return Ok(new { detail = "Test notification sent" });
    }
}
