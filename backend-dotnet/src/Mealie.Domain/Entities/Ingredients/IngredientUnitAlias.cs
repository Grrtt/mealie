namespace Mealie.Domain.Entities.Ingredients;

public class IngredientUnitAlias
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UnitId { get; set; }

    public IngredientUnit Unit { get; set; } = null!;
}
