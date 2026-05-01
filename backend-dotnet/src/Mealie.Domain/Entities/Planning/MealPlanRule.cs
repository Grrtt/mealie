using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;

namespace Mealie.Domain.Entities.Planning;

public class MealPlanRule
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }

    /// <summary>Day of week (monday/tuesday/.../sunday) or "unset" to match every day.</summary>
    public string Day { get; set; } = "unset";

    /// <summary>Meal type (breakfast/lunch/dinner/side/snack/drink/dessert) or "unset" to match all types.</summary>
    public string EntryType { get; set; } = "unset";

    /// <summary>Persisted query filter string (complex DSL from Python backend, not evaluated in C#).</summary>
    public string QueryFilterString { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Household> Households { get; set; } = [];
}
