using Mealie.Domain.Entities.Ingredients;

namespace Mealie.Domain.Entities.Recipes;

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public decimal? Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public string? OriginalText { get; set; }
    public bool IsFood { get; set; }
    public bool DisableAmount { get; set; }
    public Guid RecipeId { get; set; }

    public IngredientUnit? Unit { get; set; }
    public IngredientFood? Food { get; set; }
    public Recipe Recipe { get; set; } = null!;
}
