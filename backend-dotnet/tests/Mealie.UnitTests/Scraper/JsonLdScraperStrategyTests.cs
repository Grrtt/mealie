using Mealie.Infrastructure.Scraper;

namespace Mealie.UnitTests.Scraper;

public class JsonLdScraperStrategyTests
{
    private readonly JsonLdScraperStrategy _sut = new();

    private static string Wrap(string json) =>
        $"<html><head><script type=\"application/ld+json\">{json}</script></head></html>";

    [Fact]
    public void Scrape_DirectRecipe_ReturnsDto()
    {
        var json = """
            {
              "@context": "https://schema.org",
              "@type": "Recipe",
              "name": "Simple Cake",
              "recipeIngredient": ["2 eggs", "1 cup flour"],
              "recipeInstructions": [
                { "@type": "HowToStep", "text": "Mix." },
                { "@type": "HowToStep", "text": "Bake." }
              ],
              "prepTime": "PT10M",
              "totalTime": "PT40M"
            }
            """;
        var result = _sut.Scrape(Wrap(json));

        Assert.NotNull(result);
        Assert.Equal("Simple Cake", result.Name);
        Assert.Equal(2, result.RecipeIngredient.Count);
        Assert.Equal(2, result.RecipeInstructions.Count);
        Assert.Equal("PT10M", result.PrepTime);
        Assert.Equal("PT40M", result.TotalTime);
    }

    [Fact]
    public void Scrape_YoastGraphWithEmptyArrayNode_ReturnsRecipe()
    {
        // Mirrors real-world Yoast JSON-LD that includes [] in the @graph array
        const string json = """
            {
              "@context": "https://schema.org",
              "@graph": [
                { "@type": "WebSite", "@id": "https://example.com/#website", "name": "Example" },
                [],
                {
                  "@type": "Recipe",
                  "name": "BEST Hummus",
                  "description": "Creamy hummus recipe.",
                  "recipeYield": ["8"],
                  "prepTime": "PT5M",
                  "totalTime": "PT5M",
                  "recipeIngredient": [
                    "1\u00bd cups cooked chickpeas",
                    "\u2153 cup smooth tahini",
                    "2 tablespoons olive oil"
                  ],
                  "recipeInstructions": [
                    { "@type": "HowToStep", "text": "Blend all ingredients until smooth." },
                    { "@type": "HowToStep", "text": "Transfer and garnish." }
                  ],
                  "recipeCategory": ["Appetizer", "Snack"],
                  "keywords": "hummus, hummus recipe, what is hummus"
                }
              ]
            }
            """;
        var result = _sut.Scrape(Wrap(json));

        Assert.NotNull(result);
        Assert.Equal("BEST Hummus", result.Name);
        Assert.Equal("Creamy hummus recipe.", result.Description);
        Assert.Equal("8", result.RecipeYield);
        Assert.Equal("PT5M", result.PrepTime);
        Assert.Equal(3, result.RecipeIngredient.Count);
        Assert.Equal(2, result.RecipeInstructions.Count);
        Assert.Equal("Blend all ingredients until smooth.", result.RecipeInstructions[0]);
        Assert.Contains("Appetizer", result.Categories);
        Assert.Contains("hummus", result.Keywords);
        Assert.Contains("hummus recipe", result.Keywords);
    }

    [Fact]
    public void Scrape_NoRecipeType_ReturnsNull()
    {
        var json = """
            {
              "@context": "https://schema.org",
              "@type": "WebSite",
              "name": "Example"
            }
            """;
        var result = _sut.Scrape(Wrap(json));
        Assert.Null(result);
    }

    [Fact]
    public void Scrape_EmptyDocument_ReturnsNull()
    {
        var result = _sut.Scrape("<html></html>");
        Assert.Null(result);
    }

    [Fact]
    public void Scrape_MissingTimes_DoesNotReturnZeroDuration()
    {
        var json = """
            {
              "@context": "https://schema.org",
              "@type": "Recipe",
              "name": "Quick Snack",
              "recipeIngredient": ["1 apple"]
            }
            """;
        var result = _sut.Scrape(Wrap(json));

        Assert.NotNull(result);
        Assert.Null(result.PrepTime);
        Assert.Null(result.TotalTime);
        Assert.Null(result.CookTime);
    }
}
