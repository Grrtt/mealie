using Mealie.Application.Dtos.Groups;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Households;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households")]
public class HouseholdsController(
    IHouseholdService householdService,
    IRecipeService recipeService,
    IEmailService emailService,
    IOptions<AppSettings> settings,
    ITenantContext tenantContext) : MealieControllerBase(tenantContext)
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
    [HttpGet("members")]
    public async Task<ActionResult<IList<UserSummaryDto>>> GetMembers()
    {
        var members = await householdService.GetMembersAsync(CurrentHouseholdId);
        return Ok(members);
    }

    [HttpGet("self/statistics")]
    [HttpGet("statistics")]
    public async Task<ActionResult<HouseholdStatisticsResponse>> GetStatistics()
    {
        var stats = await householdService.GetStatisticsAsync(CurrentHouseholdId);
        return Ok(stats);
    }

    [HttpGet("self/recipes/{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetRecipeBySlug(string slug)
    {
        var recipe = await recipeService.GetDetailBySlugAsync(CurrentGroupId, slug);
        if (recipe is null) return NotFoundOrForbidden();
        return Ok(recipe);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<HouseholdPreferencesResponse>> GetPreferences()
    {
        var prefs = await householdService.GetHouseholdPreferencesAsync(CurrentHouseholdId);
        if (prefs is null) return NotFoundOrForbidden();
        return Ok(prefs);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<HouseholdPreferencesResponse>> UpdatePreferences([FromBody] UpdateHouseholdPreferencesRequest request)
    {
        var prefs = await householdService.UpdateHouseholdPreferencesAsync(CurrentHouseholdId, request);
        if (prefs is null) return NotFoundOrForbidden();
        return Ok(prefs);
    }

    [HttpPost("invitations")]
    public async Task<ActionResult<InviteTokenResponse>> CreateInvitation([FromBody] CreateInviteTokenRequest request)
    {
        var token = await householdService.CreateHouseholdInviteTokenAsync(CurrentGroupId, CurrentHouseholdId, request);
        return Ok(token);
    }

    [HttpPost("invitations/email")]
    public async Task<IActionResult> SendInvitationEmail([FromBody] HouseholdInvitationEmailRequest request)
    {
        if (emailService.IsConfigured && !string.IsNullOrEmpty(request.Email))
        {
            var baseUrl = settings.Value.BaseUrl.TrimEnd('/');
            var inviteUrl = $"{baseUrl}/register?token={Uri.EscapeDataString(request.Token)}";
            var household = await householdService.GetHouseholdAsync(CurrentHouseholdId);
            await emailService.SendInvitationEmailAsync(request.Email, household?.Name ?? "Mealie", inviteUrl);
        }
        return Ok(new { message = "Invitation email queued" });
    }

    [HttpPut("permissions")]
    public async Task<IActionResult> UpdateHouseholdPermissions([FromBody] HouseholdMemberPermissions request)
    {
        var success = await householdService.UpdateMemberPermissionsAsync(CurrentHouseholdId, request.UserId, request.Admin, request.CanOrganize, request.CanInvite);
        if (!success) return NotFoundOrForbidden();
        return Ok();
    }
}
