namespace Mealie.Api.Mcp;

public record LogEntry(
    DateTime Timestamp,
    string Level,
    string Message,
    string? Exception,
    Dictionary<string, string> Properties);
