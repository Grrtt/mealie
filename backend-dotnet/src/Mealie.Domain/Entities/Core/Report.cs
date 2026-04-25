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
