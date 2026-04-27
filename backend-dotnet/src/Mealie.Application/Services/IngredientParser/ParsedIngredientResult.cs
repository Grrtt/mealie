namespace Mealie.Application.Services.IngredientParser;

public record ParsedIngredientResult(
    string Input,
    string? Food,
    double? Quantity,
    string? Unit,
    string? Note
);
