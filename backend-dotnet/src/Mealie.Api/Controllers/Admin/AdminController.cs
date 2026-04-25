using Mealie.Application.Dtos.Admin;
using Mealie.Application.Services.Admin;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(
    IAdminUserService userService,
    IAdminGroupService groupService,
    ApplicationDbContext db) : ControllerBase
{
    // ── About & Statistics ──────────────────────────────────────────────────

    [HttpGet("about")]
    public IActionResult About() => Ok(new
    {
        production = true,
        version = "2.0.0",
        apiPort = 9000,
    });

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics(CancellationToken ct)
    {
        var userCount = await db.Users.IgnoreQueryFilters().CountAsync(ct);
        var groupCount = await db.Groups.IgnoreQueryFilters().CountAsync(ct);
        var householdCount = await db.Households.IgnoreQueryFilters().CountAsync(ct);
        var recipeCount = await db.Recipes.IgnoreQueryFilters().CountAsync(ct);

        return Ok(new
        {
            totalUsers = userCount,
            totalGroups = groupCount,
            totalHouseholds = householdCount,
            totalRecipes = recipeCount,
        });
    }

    [HttpGet("about/check")]
    public IActionResult Check() => Ok(new
    {
        emailReady = false,
        ldapReady = false,
        oidcReady = false,
        enableOpenai = false,
        baseUrlSet = true,
        isUpToDate = true,
    });

    // ── Users ───────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<ActionResult<IList<AdminUserResponse>>> GetUsers(CancellationToken ct)
        => Ok(await userService.GetAllUsersAsync(ct));

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserResponse>> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await userService.GetUserAsync(userId, ct);
        if (user is null) return NotFound(new { detail = "User not found" });
        return Ok(user);
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        [FromBody] CreateAdminUserRequest request, CancellationToken ct)
    {
        var user = await userService.CreateUserAsync(request, ct);
        return Ok(user);
    }

    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        Guid userId, [FromBody] UpdateAdminUserRequest request, CancellationToken ct)
    {
        var user = await userService.UpdateUserAsync(userId, request, ct);
        if (user is null) return NotFound(new { detail = "User not found" });
        return Ok(user);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        var deleted = await userService.DeleteUserAsync(userId, ct);
        if (!deleted) return NotFound(new { detail = "User not found" });
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid userId, CancellationToken ct)
    {
        var unlocked = await userService.UnlockUserAsync(userId, ct);
        if (!unlocked) return NotFound(new { detail = "User not found" });
        return Ok(new { detail = "User unlocked" });
    }

    // ── Groups ──────────────────────────────────────────────────────────────

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(CancellationToken ct)
        => Ok(await groupService.GetAllGroupsAsync(ct));

    [HttpGet("groups/{groupId:guid}")]
    public async Task<ActionResult<AdminGroupResponse>> GetGroup(Guid groupId, CancellationToken ct)
    {
        var group = await groupService.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound(new { detail = "Group not found" });
        return Ok(group);
    }

    [HttpPost("groups")]
    public async Task<ActionResult<AdminGroupResponse>> CreateGroup(
        [FromBody] CreateAdminGroupRequest request, CancellationToken ct)
        => Ok(await groupService.CreateGroupAsync(request, ct));

    [HttpPut("groups/{groupId:guid}")]
    public async Task<ActionResult<AdminGroupResponse>> UpdateGroup(
        Guid groupId, [FromBody] UpdateAdminGroupRequest request, CancellationToken ct)
    {
        var group = await groupService.UpdateGroupAsync(groupId, request, ct);
        if (group is null) return NotFound(new { detail = "Group not found" });
        return Ok(group);
    }

    [HttpDelete("groups/{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId, CancellationToken ct)
    {
        var deleted = await groupService.DeleteGroupAsync(groupId, ct);
        if (!deleted) return NotFound(new { detail = "Group not found" });
        return NoContent();
    }

    // ── Households ──────────────────────────────────────────────────────────

    [HttpGet("households")]
    public async Task<ActionResult<IList<AdminHouseholdResponse>>> GetHouseholds(CancellationToken ct)
        => Ok(await groupService.GetAllHouseholdsAsync(ct));

    [HttpGet("households/{householdId:guid}")]
    public async Task<ActionResult<AdminHouseholdResponse>> GetHousehold(Guid householdId, CancellationToken ct)
    {
        var household = await groupService.GetHouseholdAsync(householdId, ct);
        if (household is null) return NotFound(new { detail = "Household not found" });
        return Ok(household);
    }

    [HttpPost("households")]
    public async Task<ActionResult<AdminHouseholdResponse>> CreateHousehold(
        [FromBody] CreateAdminHouseholdRequest request, CancellationToken ct)
        => Ok(await groupService.CreateHouseholdAsync(request, ct));

    [HttpPut("households/{householdId:guid}")]
    public async Task<ActionResult<AdminHouseholdResponse>> UpdateHousehold(
        Guid householdId, [FromBody] UpdateAdminHouseholdRequest request, CancellationToken ct)
    {
        var household = await groupService.UpdateHouseholdAsync(householdId, request, ct);
        if (household is null) return NotFound(new { detail = "Household not found" });
        return Ok(household);
    }

    [HttpDelete("households/{householdId:guid}")]
    public async Task<IActionResult> DeleteHousehold(Guid householdId, CancellationToken ct)
    {
        var deleted = await groupService.DeleteHouseholdAsync(householdId, ct);
        if (!deleted) return NotFound(new { detail = "Household not found" });
        return NoContent();
    }
}
