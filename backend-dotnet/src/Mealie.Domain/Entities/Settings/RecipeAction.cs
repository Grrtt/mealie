using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Settings;

public class RecipeAction
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ActionType { get; set; } = "link";
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
}
