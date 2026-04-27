namespace Mealie.Application.Dtos.Reports;

public class ReportSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string GroupId { get; set; } = "";
    public int? TotalCount { get; set; }
    public int ProcessedCount { get; set; }
}

public class ReportEntryDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string Timestamp { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
}

public class ReportOutDto : ReportSummaryDto
{
    public List<ReportEntryDto> Entries { get; set; } = [];
}
