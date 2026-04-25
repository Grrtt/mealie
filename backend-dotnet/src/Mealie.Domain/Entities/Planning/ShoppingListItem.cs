using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;

namespace Mealie.Domain.Entities.Planning;

public class ShoppingListItem
{
    public Guid Id { get; set; }
    public string? Note { get; set; }
    public bool IsFood { get; set; }
    public bool Checked { get; set; }
    public bool DisableAmount { get; set; }
    public decimal? Quantity { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public Guid? LabelId { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;
    public IngredientUnit? Unit { get; set; }
    public IngredientFood? Food { get; set; }
    public MultiPurposeLabel? Label { get; set; }
    public ICollection<ShoppingListItemRecipeReference> RecipeReferences { get; set; } = [];
}
