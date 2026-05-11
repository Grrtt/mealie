using Mealie.Api.Mcp;
using Mealie.Application.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

/// <summary>
///     Admin endpoint for viewing recent warning/error server logs captured in memory.
///     GET /api/admin/logs
/// </summary>
[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = "admin")]
public class AdminLogsController(InMemoryLogStore logStore) : ControllerBase
{
    [HttpGet]
    public ActionResult<AdminLogsResponse> List([FromQuery] string minimumLevel = "Warning", [FromQuery] int limit = 100)
    {
        var normalizedLevel = NormalizeMinimumLevel(minimumLevel);
        if (normalizedLevel is null)
        {
            return BadRequest(new { detail = "minimumLevel must be Warning or Error." });
        }

        var boundedLimit = Math.Clamp(limit, 1, 500);
        var filteredEntries = logStore.Query(DateTime.MinValue, DateTime.MaxValue, normalizedLevel);

        var response = new AdminLogsResponse
        {
            MinimumLevel = normalizedLevel,
            Limit = boundedLimit,
            TotalCount = filteredEntries.Count,
            Entries =
            [
                .. filteredEntries
                    .TakeLast(boundedLimit)
                    .Reverse()
                    .Select(MapEntry)
            ]
        };

        return Ok(response);
    }

    private static string? NormalizeMinimumLevel(string minimumLevel)
    {
        if (string.Equals(minimumLevel, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        if (string.Equals(minimumLevel, "error", StringComparison.OrdinalIgnoreCase))
        {
            return "Error";
        }

        return null;
    }

    private static AdminLogEntryResponse MapEntry(LogEntry entry)
    {
        return new AdminLogEntryResponse
        {
            Timestamp = entry.Timestamp,
            Level = entry.Level,
            Message = entry.Message,
            Exception = entry.Exception,
            SourceContext = GetProperty(entry.Properties, "SourceContext"),
            RequestMethod = GetProperty(entry.Properties, "RequestMethod"),
            RequestPath = GetProperty(entry.Properties, "RequestPath"),
            StatusCode = GetIntProperty(entry.Properties, "StatusCode"),
            CorrelationId = GetProperty(entry.Properties, "CorrelationId"),
        };
    }

    private static string? GetProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var value) ? value : null;
    }

    private static int? GetIntProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        var value = GetProperty(properties, key);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
