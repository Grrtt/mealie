using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Settings;

public class EventNotifier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApprisUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
    public EventNotifierOptions? Options { get; set; }
}
