namespace Mealie.Domain.Entities.Ingredients;

public class IngredientFoodAlias
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid FoodId { get; set; }

    public IngredientFood Food { get; set; } = null!;
}
