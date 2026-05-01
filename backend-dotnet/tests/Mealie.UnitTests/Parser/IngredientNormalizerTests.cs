using Mealie.Infrastructure.Parser;

namespace Mealie.UnitTests.Parser;

public class IngredientNormalizerTests
{
    [Theory]
    [InlineData("▢ 1 teaspoon vegetable oil", "1 teaspoon vegetable oil")]
    [InlineData("□ 2 cups flour", "2 cups flour")]
    [InlineData("☐ salt and pepper", "salt and pepper")]
    [InlineData("☑ 3 large eggs", "3 large eggs")]
    [InlineData("◻ 1/2 cup sugar", "1/2 cup sugar")]
    [InlineData("▢▢ 1 cup milk", "1 cup milk")] // multiple leading checkboxes
    [InlineData("1 teaspoon vanilla", "1 teaspoon vanilla")] // no checkbox — unchanged
    [InlineData("  ▢  1 cup water  ", "1 cup water")] // leading/trailing whitespace
    [InlineData("1  cup   flour", "1 cup flour")] // internal whitespace collapsed
    public void Normalize_StripsCheckboxAndNormalizesWhitespace(string input, string expected)
    {
        Assert.Equal(expected, IngredientNormalizer.Normalize(input));
    }
}
