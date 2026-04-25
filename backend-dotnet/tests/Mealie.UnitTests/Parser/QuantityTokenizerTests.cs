using Mealie.Infrastructure.Parser;

namespace Mealie.UnitTests.Parser;

public class QuantityTokenizerTests
{
    [Theory]
    [InlineData("1 cup flour", 1.0, "cup flour")]
    [InlineData("2.5 cups water", 2.5, "cups water")]
    [InlineData("1/2 tsp salt", 0.5, "tsp salt")]
    [InlineData("1 1/2 cups milk", 1.5, "cups milk")]
    [InlineData("½ cup sugar", 0.5, "cup sugar")]
    [InlineData("¼ tsp pepper", 0.25, "tsp pepper")]
    [InlineData("1-2 cloves garlic", 1.5, "cloves garlic")]
    [InlineData("3 large eggs", 3.0, "large eggs")]
    [InlineData("salt and pepper", null, "salt and pepper")]
    public void Tokenize_ReturnsCorrectQuantityAndRemainder(string input, double? expectedQty, string expectedRemainder)
    {
        var (qty, remainder) = QuantityTokenizer.Tokenize(input);
        if (expectedQty is null)
            Assert.Null(qty);
        else
            Assert.Equal((decimal)expectedQty, qty!.Value, 5);
        Assert.Equal(expectedRemainder, remainder);
    }
}
