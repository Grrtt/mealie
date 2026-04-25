using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Ingredients;

public class IngredientFood
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PluralName { get; set; }
    public Guid? UnitId { get; set; }
    public Guid GroupId { get; set; }
    public bool OnHand { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public IngredientUnit? Unit { get; set; }
    public Group Group { get; set; } = null!;
    public ICollection<IngredientFoodAlias> Aliases { get; set; } = [];
}
