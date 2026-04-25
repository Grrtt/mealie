namespace Mealie.Application.Dtos.Recipes;

public class RecipeSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int? Rating { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public IList<OrganizerSimpleResponse> Tags { get; set; } = [];
    public IList<OrganizerSimpleResponse> Categories { get; set; } = [];
}

public class RecipeDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RecipeYield { get; set; }
    public string? TotalTime { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? PerformTime { get; set; }
    public int? Rating { get; set; }
    public bool DisableAmount { get; set; }
    public string? Image { get; set; }
    public string? OrgUrl { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public DateTime? LastMade { get; set; }
    public NutritionDto? Nutrition { get; set; }
    public RecipeSettingsDto? Settings { get; set; }
    public IList<RecipeIngredientDto> RecipeIngredients { get; set; } = [];
    public IList<RecipeInstructionDto> RecipeInstructions { get; set; } = [];
    public IList<RecipeNoteDto> Notes { get; set; } = [];
    public IList<RecipeAssetDto> Assets { get; set; } = [];
    public IList<OrganizerSimpleResponse> Tags { get; set; } = [];
    public IList<OrganizerSimpleResponse> Categories { get; set; } = [];
    public IList<OrganizerSimpleResponse> Tools { get; set; } = [];
}

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateRecipeRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? RecipeYield { get; set; }
    public string? TotalTime { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? PerformTime { get; set; }
    public int? Rating { get; set; }
    public bool? DisableAmount { get; set; }
    public string? OrgUrl { get; set; }
    public DateTime? LastMade { get; set; }
    public NutritionDto? Nutrition { get; set; }
    public RecipeSettingsDto? Settings { get; set; }
    public IList<RecipeIngredientDto>? RecipeIngredients { get; set; }
    public IList<RecipeInstructionDto>? RecipeInstructions { get; set; }
    public IList<RecipeNoteDto>? Notes { get; set; }
    public IList<string>? Tags { get; set; }
    public IList<string>? Categories { get; set; }
    public IList<string>? Tools { get; set; }
}

public class NutritionDto
{
    public string? Calories { get; set; }
    public string? FatContent { get; set; }
    public string? ProteinContent { get; set; }
    public string? CarbohydrateContent { get; set; }
    public string? FiberContent { get; set; }
    public string? SodiumContent { get; set; }
    public string? SugarContent { get; set; }
}

public class RecipeSettingsDto
{
    public bool Public { get; set; }
    public bool ShowNutrition { get; set; }
    public bool ShowAssets { get; set; }
    public bool LandscapeView { get; set; }
    public bool DisableComments { get; set; }
    public bool DisableAmount { get; set; }
    public bool Locked { get; set; }
}

public class RecipeIngredientDto
{
    public Guid? Id { get; set; }
    public int Position { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public decimal? Quantity { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? FoodId { get; set; }
    public string? OriginalText { get; set; }
    public bool IsFood { get; set; }
    public bool DisableAmount { get; set; }
    public string? UnitName { get; set; }
    public string? FoodName { get; set; }
}

public class RecipeInstructionDto
{
    public Guid? Id { get; set; }
    public int Position { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Summary { get; set; }
}

public class RecipeNoteDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class RecipeAssetDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? FileName { get; set; }
}

public class OrganizerSimpleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class RecipeScraperRequest
{
    public string Url { get; set; } = string.Empty;
    public bool IncludeTags { get; set; } = true;
}

public class BulkScrapeRequest
{
    public IList<string> Urls { get; set; } = [];
    public bool IncludeTags { get; set; } = true;
}

public class ExportFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}
