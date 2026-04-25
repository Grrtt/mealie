using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Settings;

public class GroupPreferences
{
    public Guid Id { get; set; }
    public bool PrivateGroup { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
}
