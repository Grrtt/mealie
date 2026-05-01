using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Ingredients;

public class IngredientUnit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Abbreviation { get; set; }
    public string? PluralName { get; set; }
    public string? PluralAbbreviation { get; set; }
    public bool UseAbbreviation { get; set; }
    public bool Fraction { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<IngredientFood> Foods { get; set; } = [];
    public ICollection<IngredientUnitAlias> Aliases { get; set; } = [];
}
