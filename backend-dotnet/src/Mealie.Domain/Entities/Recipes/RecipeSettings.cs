namespace Mealie.Domain.Entities.Recipes;

public class RecipeSettings
{
    public bool Public { get; set; }
    public bool ShowNutrition { get; set; }
    public bool ShowAssets { get; set; }
    public bool LandscapeView { get; set; }
    public bool DisableComments { get; set; }
    public bool DisableAmount { get; set; }
    public bool Locked { get; set; }
}
