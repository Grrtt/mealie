using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Recipes;

namespace Mealie.Domain.Entities.Planning;

public class MealPlan
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public Guid? RecipeId { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Recipe? Recipe { get; set; }
    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
    public User User { get; set; } = null!;
}
