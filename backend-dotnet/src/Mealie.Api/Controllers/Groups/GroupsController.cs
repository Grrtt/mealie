using Mealie.Application.Dtos.Groups;
using Mealie.Application.Dtos.Reports;
using Mealie.Application.Services.Groups;
using Mealie.Application.Services.Migrations;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Groups;

[ApiController]
[Route("api/groups")]
public class GroupsController(
    IGroupService groupService,
    MigrationQueue migrationQueue,
    ApplicationDbContext db,
    ITenantContext tenantContext) : MealieControllerBase(tenantContext)
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

    [HttpGet("members")]
    [HttpGet("self/members")]
    public async Task<ActionResult<object>> GetMembers()
    {
        var members = await groupService.GetMembersAsync(CurrentGroupId);
        return Ok(new { items = members, total = members.Count, page = 1, perPage = -1 });
    }

    [HttpGet("households")]
    public async Task<ActionResult<object>> GetHouseholds()
    {
        var households = await groupService.GetHouseholdsAsync(CurrentGroupId);
        return Ok(new { items = households, total = households.Count, page = 1, perPage = -1 });
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

    // ── Migrations ─────────────────────────────────────────────────────────

    [HttpPost("migrations")]
    public async Task<ActionResult<ReportSummaryDto>> ImportRecipes(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "No file uploaded" });

        // Buffer the upload to a temp file so the request can complete
        // while the background service processes at its own pace.
        var tempPath = Path.Combine(Path.GetTempPath(), $"mealie-migration-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
        await using (var fs = System.IO.File.Create(tempPath))
            await file.CopyToAsync(fs);

        // Create a DB report record with "in-progress" status and return it immediately.
        var report = new Report
        {
            Id = Guid.NewGuid(),
            Name = $"Migration — {file.FileName}",
            Category = "migration",
            Status = "in-progress",
            Timestamp = DateTime.UtcNow,
            GroupId = CurrentGroupId,
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        await migrationQueue.EnqueueAsync(new MigrationJobRequest(
            report.Id, CurrentGroupId, CurrentHouseholdId, CurrentUserId, tempPath));

        return Ok(MapReportSummary(report));
    }

    // ── Reports ────────────────────────────────────────────────────────────

    [HttpGet("reports")]
    public async Task<ActionResult<IList<ReportSummaryDto>>> GetReports([FromQuery(Name = "report_type")] string? reportType)
    {
        var query = db.Reports.IgnoreQueryFilters()
            .Where(r => r.GroupId == CurrentGroupId);

        if (!string.IsNullOrEmpty(reportType))
            query = query.Where(r => r.Category == reportType);

        var reports = await query
            .OrderByDescending(r => r.Timestamp)
            .Select(r => MapReportSummary(r))
            .ToListAsync();

        return Ok(reports);
    }

    [HttpGet("reports/{reportId:guid}")]
    public async Task<ActionResult<ReportOutDto>> GetReport(Guid reportId)
    {
        var report = await db.Reports.IgnoreQueryFilters()
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.GroupId == CurrentGroupId);

        if (report is null) return NotFoundOrForbidden();

        return Ok(new ReportOutDto
        {
            Id = report.Id,
            Name = report.Name,
            Category = report.Category,
            Status = report.Status,
            Timestamp = report.Timestamp.ToString("o"),
            GroupId = report.GroupId.ToString(),
            Entries = report.Entries.Select(e => new ReportEntryDto
            {
                Id = e.Id,
                ReportId = e.ReportId,
                Timestamp = e.Timestamp.ToString("o"),
                Success = e.Success,
                Message = e.Message,
                Exception = e.Exception,
            }).ToList(),
        });
    }

    [HttpDelete("reports/{reportId:guid}")]
    public async Task<IActionResult> DeleteReport(Guid reportId)
    {
        var report = await db.Reports.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == reportId && r.GroupId == CurrentGroupId);

        if (report is null) return NotFoundOrForbidden();

        db.Reports.Remove(report);
        await db.SaveChangesAsync();
        return Ok();
    }

    private static ReportSummaryDto MapReportSummary(Report r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Category = r.Category,
        Status = r.Status,
        Timestamp = r.Timestamp.ToString("o"),
        GroupId = r.GroupId.ToString(),
    };
}
