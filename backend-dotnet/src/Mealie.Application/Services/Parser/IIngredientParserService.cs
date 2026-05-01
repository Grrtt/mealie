namespace Mealie.Application.Services.Parser;

public class IngredientConfidenceDto
{
    public double? Average { get; set; }
    public double? Comment { get; set; }
    public double? Name { get; set; }
    public double? Unit { get; set; }
    public double? Quantity { get; set; }
    public double? Food { get; set; }
}

public class ParsedIngredientDto
{
    public string? Input { get; set; }
    public IngredientConfidenceDto Confidence { get; set; } = new();
    public ParsedIngredientIngredientDto Ingredient { get; set; } = new();
}

public class ParsedIngredientIngredientDto
{
    public decimal? Quantity { get; set; }
    public ParsedIngredientUnitDto? Unit { get; set; }
    public ParsedIngredientFoodDto? Food { get; set; }
    public string? Note { get; set; }
    public string? Display { get; set; }
    public string? Title { get; set; }
    public string? OriginalText { get; set; }
    public string? ReferenceId { get; set; }
}

public class ParsedIngredientUnitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ParsedIngredientFoodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IIngredientParserService
{
    Task<ParsedIngredientDto> ParseAsync(Guid groupId, string ingredientString, CancellationToken ct = default);

    Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients,
        CancellationToken ct = default);
}
