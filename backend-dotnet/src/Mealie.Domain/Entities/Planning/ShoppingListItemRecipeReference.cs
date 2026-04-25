using Mealie.Domain.Entities.Recipes;

namespace Mealie.Domain.Entities.Planning;

public class ShoppingListItemRecipeReference
{
    public Guid Id { get; set; }
    public Guid ShoppingListItemId { get; set; }
    public Guid RecipeId { get; set; }
    public Guid? RecipeQuantityId { get; set; }
    public decimal RecipeQuantity { get; set; }
    public decimal RecipeScale { get; set; }

    public ShoppingListItem ShoppingListItem { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
