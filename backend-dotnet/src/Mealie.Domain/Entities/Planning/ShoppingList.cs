using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Planning;

public class ShoppingList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
    public ICollection<ShoppingListItem> Items { get; set; } = [];
    public ICollection<ShoppingListRecipeReference> RecipeReferences { get; set; } = [];
}
