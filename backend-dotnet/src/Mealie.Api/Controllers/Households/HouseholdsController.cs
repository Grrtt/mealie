using Mealie.Application.Commands.Groups;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Groups;
using Mealie.Application.Queries.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households")]
public class HouseholdsController(
    QueryExecutor executor,
    IEmailService emailService,
    IOptions<AppSettings> settings,
    ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet("self")]
    public async Task<ActionResult<HouseholdResponse>> GetSelf(CancellationToken ct = default)
    {
        var h = await executor.ExecuteAsync(new GetHouseholdQuery(CurrentHouseholdId), ct);
        if (h is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(h);
    }

    [HttpPut("self")]
    public async Task<ActionResult<HouseholdResponse>> UpdateSelf([FromBody] UpdateHouseholdRequest request,
        CancellationToken ct = default)
    {
        var h = await executor.ExecuteAsync(new UpdateHouseholdCommand(CurrentHouseholdId, request), ct);
        if (h is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(h);
    }

    [HttpGet("self/members")]
    [HttpGet("members")]
    public async Task<ActionResult<IList<UserSummaryDto>>> GetMembers(CancellationToken ct = default)
    {
        return Ok(await executor.ExecuteAsync(new GetHouseholdMembersQuery(CurrentHouseholdId), ct));
    }

    [HttpGet("self/statistics")]
    [HttpGet("statistics")]
    public async Task<ActionResult<HouseholdStatisticsResponse>> GetStatistics(CancellationToken ct = default)
    {
        return Ok(await executor.ExecuteAsync(new GetHouseholdStatisticsQuery(CurrentHouseholdId), ct));
    }

    [HttpGet("self/recipes/{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetRecipeBySlug(string slug, CancellationToken ct = default)
    {
        var recipe = await executor.ExecuteAsync(new GetRecipeDetailBySlugQuery(CurrentGroupId, slug), ct);
        if (recipe is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(recipe);
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<HouseholdPreferencesResponse>> GetPreferences(CancellationToken ct = default)
    {
        var prefs = await executor.ExecuteAsync(new GetHouseholdPreferencesQuery(CurrentHouseholdId), ct);
        if (prefs is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(prefs);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<HouseholdPreferencesResponse>> UpdatePreferences(
        [FromBody] UpdateHouseholdPreferencesRequest request, CancellationToken ct = default)
    {
        var prefs = await executor.ExecuteAsync(new UpdateHouseholdPreferencesCommand(CurrentHouseholdId, request), ct);
        if (prefs is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(prefs);
    }

    [HttpPost("invitations")]
    public async Task<ActionResult<InviteTokenResponse>> CreateInvitation([FromBody] CreateInviteTokenRequest request,
        CancellationToken ct = default)
    {
        return Ok(await executor.ExecuteAsync(
            new CreateHouseholdInviteTokenCommand(CurrentGroupId, CurrentHouseholdId, request), ct));
    }

    [HttpPost("invitations/email")]
    public async Task<IActionResult> SendInvitationEmail([FromBody] HouseholdInvitationEmailRequest request,
        CancellationToken ct = default)
    {
        if (emailService.IsConfigured && !string.IsNullOrEmpty(request.Email))
        {
            var baseUrl = settings.Value.BaseUrl.TrimEnd('/');
            var inviteUrl = $"{baseUrl}/register?token={Uri.EscapeDataString(request.Token)}";
            var household = await executor.ExecuteAsync(new GetHouseholdQuery(CurrentHouseholdId), ct);
            await emailService.SendInvitationEmailAsync(request.Email, household?.Name ?? "Mealie", inviteUrl);
        }

        return Ok(new { message = "Invitation email queued" });
    }

    [HttpPut("permissions")]
    public async Task<IActionResult> UpdateHouseholdPermissions([FromBody] HouseholdMemberPermissions request,
        CancellationToken ct = default)
    {
        var success = await executor.ExecuteAsync(new UpdateHouseholdMemberPermissionsCommand(
            CurrentHouseholdId, request.UserId, request.Admin, request.CanOrganize, request.CanInvite), ct);
        if (!success)
        {
            return NotFoundOrForbidden();
        }

        return Ok();
    }
}
