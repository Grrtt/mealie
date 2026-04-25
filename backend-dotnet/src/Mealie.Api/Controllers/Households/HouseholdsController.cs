using Mealie.Application.Dtos.Groups;
using Mealie.Application.Services.Households;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households")]
public class HouseholdsController(IHouseholdService householdService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet("self")]
    public async Task<ActionResult<HouseholdResponse>> GetSelf()
    {
        var h = await householdService.GetHouseholdAsync(CurrentHouseholdId);
        if (h is null) return NotFoundOrForbidden();
        return Ok(h);
    }

    [HttpPut("self")]
    public async Task<ActionResult<HouseholdResponse>> UpdateSelf([FromBody] UpdateHouseholdRequest request)
    {
        var h = await householdService.UpdateHouseholdAsync(CurrentHouseholdId, request);
        if (h is null) return NotFoundOrForbidden();
        return Ok(h);
    }

    [HttpGet("self/members")]
    public async Task<ActionResult<IList<UserSummaryDto>>> GetMembers()
    {
        var members = await householdService.GetMembersAsync(CurrentHouseholdId);
        return Ok(members);
    }

    [HttpGet("self/statistics")]
    public async Task<ActionResult<HouseholdStatisticsResponse>> GetStatistics()
    {
        var stats = await householdService.GetStatisticsAsync(CurrentHouseholdId);
        return Ok(stats);
    }
}
