using Mealie.Domain.Entities.Recipes;

namespace Mealie.Domain.Entities.Planning;

public class ShoppingListRecipeReference
{
    public Guid Id { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid RecipeId { get; set; }
    public decimal RecipeScale { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
