namespace Mealie.Domain.Entities.Core;

public class Report
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "migration";
    public string Status { get; set; } = "in-progress";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public List<ReportEntry> Entries { get; set; } = [];

    /// <summary>Path to the queued migration zip on the persistent data volume. Null once job completes.</summary>
    public string? QueuedFilePath { get; set; }

    /// <summary>Context needed to re-queue the job on startup after a crash/restart.</summary>
    public Guid? QueuedHouseholdId { get; set; }
    public Guid? QueuedUserId { get; set; }

    /// <summary>Total recipes discovered in the archive. Null until processing begins.</summary>
    public int? TotalCount { get; set; }

    /// <summary>Number of recipes processed so far (created + skipped + errors).</summary>
    public int ProcessedCount { get; set; }
}

public class ReportEntry
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Report Report { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
}
