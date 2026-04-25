namespace Mealie.Application.Services.Parser;

public class ParsedIngredientDto
{
    public string Input { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public ParsedIngredientUnitDto? Unit { get; set; }
    public ParsedIngredientFoodDto? Food { get; set; }
    public string? Note { get; set; }
    public string Confidence { get; set; } = "low";
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
    Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients, CancellationToken ct = default);
}
