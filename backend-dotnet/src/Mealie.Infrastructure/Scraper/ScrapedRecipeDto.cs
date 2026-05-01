namespace Mealie.Infrastructure.Scraper;

public class ScrapedRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? RecipeYield { get; set; }
    public string? TotalTime { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public IList<string> RecipeIngredient { get; set; } = [];
    public IList<string> RecipeInstructions { get; set; } = [];
    public IList<string> Keywords { get; set; } = [];
    public IList<string> Categories { get; set; } = [];
    public string? OrgUrl { get; set; }
    public NutritionDto? Nutrition { get; set; }
    public bool ScrapingNotSupported { get; set; }

    /// <summary>
    ///     Image files extracted from the source archive: filename → raw bytes.
    ///     Written to {DataDir}/recipes/{slug}/images/ after the recipe is created.
    /// </summary>
    public Dictionary<string, byte[]> ImageFiles { get; set; } = [];
}

public class NutritionDto
{
    public string? Calories { get; set; }
    public string? FatContent { get; set; }
    public string? ProteinContent { get; set; }
    public string? CarbohydrateContent { get; set; }
}
