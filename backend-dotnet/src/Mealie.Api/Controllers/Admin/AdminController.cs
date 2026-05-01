using Mealie.Application.Commands.Admin;
using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Admin;
using Mealie.Application.Services.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(
    QueryExecutor executor,
    IPasswordResetService passwordResetService,
    IEmailService emailService,
    IOptions<AppSettings> settings,
    ApplicationDbContext db) : ControllerBase
{
    // ── About & Statistics ──────────────────────────────────────────────────

    [HttpGet("about")]
    public IActionResult About()
    {
        return Ok(new
        {
            production = true,
            version = "2.0.0",
            apiPort = settings.Value.ApiPort
        });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics(CancellationToken ct)
    {
        var userCount = await db.Users.IgnoreQueryFilters().CountAsync(ct);
        var groupCount = await db.Groups.IgnoreQueryFilters().CountAsync(ct);
        var householdCount = await db.Households.IgnoreQueryFilters().CountAsync(ct);
        var recipeCount = await db.Recipes.IgnoreQueryFilters().CountAsync(ct);

        return Ok(new
        {
            totalUsers = userCount, totalGroups = groupCount, totalHouseholds = householdCount,
            totalRecipes = recipeCount
        });
    }

    [HttpGet("about/statistics")]
    public async Task<IActionResult> AboutStatistics(CancellationToken ct)
    {
        return await Statistics(ct);
    }

    [HttpGet("about/check")]
    public IActionResult Check()
    {
        return Ok(new
        {
            emailReady = emailService.IsConfigured,
            ldapReady = settings.Value.LdapEnabled && !string.IsNullOrEmpty(settings.Value.LdapServer),
            oidcReady = settings.Value.OidcEnabled && !string.IsNullOrEmpty(settings.Value.OidcAuthority),
            enableOpenai = !string.IsNullOrEmpty(settings.Value.OpenAiApiKey),
            baseUrlSet = !string.IsNullOrEmpty(settings.Value.BaseUrl),
            isUpToDate = true
        });
    }

    [HttpGet("about/docker/validate")]
    public IActionResult ValidateDocker()
    {
        return Ok(new { message = "ok" });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        var userCount = await db.Users.IgnoreQueryFilters().CountAsync(ct);
        var groupCount = await db.Groups.IgnoreQueryFilters().CountAsync(ct);
        var householdCount = await db.Households.IgnoreQueryFilters().CountAsync(ct);
        var recipeCount = await db.Recipes.IgnoreQueryFilters().CountAsync(ct);

        return Ok(new AnalyticsResponse
        {
            TotalUsers = userCount,
            TotalGroups = groupCount,
            TotalHouseholds = householdCount,
            TotalRecipes = recipeCount
        });
    }

    // ── Users ───────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetAllUsersQuery(), ct));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserResponse>> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await executor.ExecuteAsync(new GetAdminUserQuery(userId), ct);
        if (user is null)
        {
            return NotFound(new { detail = "User not found" });
        }

        return Ok(user);
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        [FromBody] CreateAdminUserRequest request, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new CreateAdminUserCommand(request), ct));
    }

    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        Guid userId, [FromBody] UpdateAdminUserRequest request, CancellationToken ct)
    {
        var user = await executor.ExecuteAsync(new UpdateAdminUserCommand(userId, request), ct);
        if (user is null)
        {
            return NotFound(new { detail = "User not found" });
        }

        return Ok(user);
    }

    [HttpPatch("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserResponse>> PatchUser(
        Guid userId, [FromBody] UpdateAdminUserRequest request, CancellationToken ct)
    {
        return await UpdateUser(userId, request, ct);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteAdminUserCommand(userId), ct);
        if (!deleted)
        {
            return NotFound(new { detail = "User not found" });
        }

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid userId, CancellationToken ct)
    {
        var unlocked = await executor.ExecuteAsync(new UnlockUserCommand(userId), ct);
        if (!unlocked)
        {
            return NotFound(new { detail = "User not found" });
        }

        return Ok(new { detail = "User unlocked" });
    }

    [HttpPost("users/password-reset-token")]
    public async Task<IActionResult> GeneratePasswordResetToken(
        [FromBody] PasswordResetTokenRequest request, CancellationToken ct)
    {
        var email = request.Email;

        if (request.UserId.HasValue && request.UserId != Guid.Empty)
        {
            var user = await db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
            if (user is null)
            {
                return NotFound(new { detail = "User not found" });
            }

            email = user.Email;
        }

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { detail = "Either userId or email must be provided" });
        }

        var token = await passwordResetService.GenerateResetTokenAsync(email);
        if (token is null)
        {
            return NotFound(new { detail = "User with that email not found" });
        }

        return Ok(new PasswordResetTokenResponse { Token = token });
    }

    // ── Groups ──────────────────────────────────────────────────────────────

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetAllGroupsQuery(), ct));
    }

    [HttpGet("groups/{groupId:guid}")]
    public async Task<ActionResult<AdminGroupResponse>> GetGroup(Guid groupId, CancellationToken ct)
    {
        var group = await executor.ExecuteAsync(new GetAdminGroupQuery(groupId), ct);
        if (group is null)
        {
            return NotFound(new { detail = "Group not found" });
        }

        return Ok(group);
    }

    [HttpPost("groups")]
    public async Task<ActionResult<AdminGroupResponse>> CreateGroup(
        [FromBody] CreateAdminGroupRequest request, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new CreateAdminGroupCommand(request), ct));
    }

    [HttpPut("groups/{groupId:guid}")]
    public async Task<ActionResult<AdminGroupResponse>> UpdateGroup(
        Guid groupId, [FromBody] UpdateAdminGroupRequest request, CancellationToken ct)
    {
        var group = await executor.ExecuteAsync(new UpdateAdminGroupCommand(groupId, request), ct);
        if (group is null)
        {
            return NotFound(new { detail = "Group not found" });
        }

        return Ok(group);
    }

    [HttpPatch("groups/{groupId:guid}")]
    public async Task<ActionResult<AdminGroupResponse>> PatchGroup(
        Guid groupId, [FromBody] UpdateAdminGroupRequest request, CancellationToken ct)
    {
        return await UpdateGroup(groupId, request, ct);
    }

    [HttpDelete("groups/{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteAdminGroupCommand(groupId), ct);
        if (!deleted)
        {
            return NotFound(new { detail = "Group not found" });
        }

        return NoContent();
    }

    // ── Households ──────────────────────────────────────────────────────────

    [HttpGet("households")]
    public async Task<IActionResult> GetHouseholds(CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetAllHouseholdsQuery(), ct));
    }

    [HttpGet("households/{householdId:guid}")]
    public async Task<ActionResult<AdminHouseholdResponse>> GetHousehold(Guid householdId, CancellationToken ct)
    {
        var household = await executor.ExecuteAsync(new GetAdminHouseholdQuery(householdId), ct);
        if (household is null)
        {
            return NotFound(new { detail = "Household not found" });
        }

        return Ok(household);
    }

    [HttpPost("households")]
    public async Task<ActionResult<AdminHouseholdResponse>> CreateHousehold(
        [FromBody] CreateAdminHouseholdRequest request, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new CreateAdminHouseholdCommand(request), ct));
    }

    [HttpPut("households/{householdId:guid}")]
    public async Task<ActionResult<AdminHouseholdResponse>> UpdateHousehold(
        Guid householdId, [FromBody] UpdateAdminHouseholdRequest request, CancellationToken ct)
    {
        var household = await executor.ExecuteAsync(new UpdateAdminHouseholdCommand(householdId, request), ct);
        if (household is null)
        {
            return NotFound(new { detail = "Household not found" });
        }

        return Ok(household);
    }

    [HttpPatch("households/{householdId:guid}")]
    public async Task<ActionResult<AdminHouseholdResponse>> PatchHousehold(
        Guid householdId, [FromBody] UpdateAdminHouseholdRequest request, CancellationToken ct)
    {
        return await UpdateHousehold(householdId, request, ct);
    }

    [HttpDelete("households/{householdId:guid}")]
    public async Task<IActionResult> DeleteHousehold(Guid householdId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteAdminHouseholdCommand(householdId), ct);
        if (!deleted)
        {
            return NotFound(new { detail = "Household not found" });
        }

        return NoContent();
    }
}
