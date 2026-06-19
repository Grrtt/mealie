using Mealie.Domain.Entities.Organizers;

namespace Mealie.Domain.Entities.Planning;

public class ShoppingListLabel
{
    public Guid Id { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid LabelId { get; set; }
    public int Position { get; set; }

    // Navigation
    public ShoppingList ShoppingList { get; set; } = null!;
    public MultiPurposeLabel Label { get; set; } = null!;
}
