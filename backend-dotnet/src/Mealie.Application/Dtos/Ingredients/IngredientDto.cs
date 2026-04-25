namespace Mealie.Application.Dtos.Ingredients;

public class FoodResponse
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
}

public class UnitResponse
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
}

public class CreateFoodRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PluralName { get; set; }
    public Guid? UnitId { get; set; }
    public bool OnHand { get; set; }
}

public class UpdateFoodRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PluralName { get; set; }
    public Guid? UnitId { get; set; }
    public bool? OnHand { get; set; }
}

public class CreateUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Abbreviation { get; set; }
    public string? PluralName { get; set; }
    public string? PluralAbbreviation { get; set; }
    public bool UseAbbreviation { get; set; }
    public bool Fraction { get; set; }
}

public class UpdateUnitRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Abbreviation { get; set; }
    public string? PluralName { get; set; }
    public string? PluralAbbreviation { get; set; }
    public bool? UseAbbreviation { get; set; }
    public bool? Fraction { get; set; }
}
