using Mealie.Application.Dtos.Groups;
using Mealie.Application.Services.Groups;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Groups;

[ApiController]
[Route("api/groups")]
public class GroupsController(IGroupService groupService, MigrationImportService migrationImportService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet("self")]
    public async Task<ActionResult<GroupResponse>> GetSelf()
    {
        var group = await groupService.GetGroupAsync(CurrentGroupId);
        if (group is null) return NotFoundOrForbidden();
        return Ok(group);
    }

    [HttpPut("self")]
    public async Task<ActionResult<GroupResponse>> UpdateSelf([FromBody] UpdateGroupRequest request)
    {
        var group = await groupService.UpdateGroupAsync(CurrentGroupId, request);
        if (group is null) return NotFoundOrForbidden();
        return Ok(group);
    }

    [HttpGet("self/members")]
    public async Task<ActionResult<IList<UserSummaryDto>>> GetMembers()
    {
        var members = await groupService.GetMembersAsync(CurrentGroupId);
        return Ok(members);
    }

    [HttpGet("self/invitations")]
    public async Task<ActionResult<IList<InviteTokenResponse>>> GetInvitations()
    {
        var tokens = await groupService.GetInviteTokensAsync(CurrentGroupId);
        return Ok(tokens);
    }

    [HttpPost("self/invitations")]
    public async Task<ActionResult<InviteTokenResponse>> CreateInvitation([FromBody] CreateInviteTokenRequest request)
    {
        var token = await groupService.CreateInviteTokenAsync(CurrentGroupId, request.HouseholdId);
        return Ok(token);
    }

    [HttpDelete("self/invitations/{tokenId:guid}")]
    public async Task<IActionResult> DeleteInvitation(Guid tokenId)
    {
        var success = await groupService.DeleteInviteTokenAsync(CurrentGroupId, tokenId);
        if (!success) return NotFoundOrForbidden();
        return Ok();
    }

    [HttpPost("migrations")]
    public async Task<IActionResult> ImportRecipes(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "No file uploaded" });

        using var stream = file.OpenReadStream();
        var report = await migrationImportService.ImportAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, stream);

        if (report.Error is not null)
            return BadRequest(new { detail = report.Error });

        return Ok(report);
    }
}
