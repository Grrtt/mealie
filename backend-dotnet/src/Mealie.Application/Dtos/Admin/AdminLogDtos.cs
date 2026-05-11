namespace Mealie.Application.Dtos.Admin;

public class AdminLogsResponse
{
    public string MinimumLevel { get; init; } = "Warning";
    public int Limit { get; init; }
    public int TotalCount { get; init; }
    public List<AdminLogEntryResponse> Entries { get; init; } = [];
}

public class AdminLogEntryResponse
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public string? SourceContext { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPath { get; init; }
    public int? StatusCode { get; init; }
    public string? CorrelationId { get; init; }
}
