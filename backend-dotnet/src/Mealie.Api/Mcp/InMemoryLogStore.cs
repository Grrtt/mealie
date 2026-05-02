using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Mealie.Api.Mcp;

public class InMemoryLogStore : ILogEventSink
{
    private static readonly Dictionary<string, int> LevelOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Verbose"] = 0,
        ["Debug"] = 1,
        ["Information"] = 2,
        ["Warning"] = 3,
        ["Error"] = 4,
        ["Fatal"] = 5,
    };

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private const int MaxEntries = 10_000;

    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry(
            logEvent.Timestamp.UtcDateTime,
            logEvent.Level.ToString(),
            logEvent.RenderMessage(),
            logEvent.Exception?.ToString(),
            logEvent.Properties.ToDictionary(
                p => p.Key,
                p => p.Value.ToString().Trim('"')));

        _entries.Enqueue(entry);

        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
    }

    public IReadOnlyList<LogEntry> Query(DateTime from, DateTime to, string? minimumLevel = null)
    {
        var minOrdinal = minimumLevel is not null && LevelOrder.TryGetValue(minimumLevel, out var ord) ? ord : 0;

        return [.. _entries.Where(e =>
            e.Timestamp >= from &&
            e.Timestamp <= to &&
            LevelOrder.GetValueOrDefault(e.Level, 0) >= minOrdinal)];
    }
}
