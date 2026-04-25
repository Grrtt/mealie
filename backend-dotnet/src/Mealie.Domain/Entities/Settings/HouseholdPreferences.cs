using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Settings;

public class HouseholdPreferences
{
    public Guid Id { get; set; }
    public bool PrivateHousehold { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? RecipePublic { get; set; }
    public string? RecipeShowNutrition { get; set; }
    public string? RecipeShowAssets { get; set; }
    public string? RecipeLandscapeView { get; set; }
    public string? RecipeDisableComments { get; set; }
    public string? RecipeDisableAmount { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Household Household { get; set; } = null!;
}
