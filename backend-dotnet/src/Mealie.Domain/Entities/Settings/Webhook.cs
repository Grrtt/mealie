using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Settings;

public class Webhook
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? ScheduledTime { get; set; }
    public string? Method { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
}
