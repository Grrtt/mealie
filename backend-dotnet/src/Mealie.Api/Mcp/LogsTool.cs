using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mealie.Api.Mcp;

[McpServerToolType]
public class LogsTool(InMemoryLogStore logStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [McpServerTool(Name = "get_logs")]
    [Description("Get application log entries within a specified time range.")]
    public string GetLogs(
        [Description("Start of the time range in ISO 8601 UTC format (e.g. 2024-01-01T00:00:00Z)")]
        DateTime from,
        [Description("End of the time range in ISO 8601 UTC format. Defaults to current UTC time if omitted.")]
        DateTime? to,
        [Description("Minimum log level to include: Verbose, Debug, Information, Warning, Error, Fatal. Defaults to Information.")]
        string? minimumLevel)
    {
        var end = to ?? DateTime.UtcNow;
        var level = minimumLevel ?? "Information";
        var entries = logStore.Query(from, end, level);
        return JsonSerializer.Serialize(new { count = entries.Count, entries }, JsonOptions);
    }
}
