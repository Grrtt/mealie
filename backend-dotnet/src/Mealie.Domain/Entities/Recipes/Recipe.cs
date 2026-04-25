using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;

namespace Mealie.Domain.Entities.Recipes;

public class Recipe
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
    public bool IsOcr { get; set; }
    public bool DisableAmount { get; set; }
    public string? Image { get; set; }
    public string? OrgUrl { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public DateTime? LastMade { get; set; }

    public Nutrition? Nutrition { get; set; }
    public RecipeSettings? Settings { get; set; }
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public ICollection<RecipeInstruction> RecipeInstructions { get; set; } = [];
    public ICollection<RecipeNote> Notes { get; set; } = [];
    public ICollection<RecipeAsset> Assets { get; set; } = [];
    public ICollection<RecipeComment> Comments { get; set; } = [];
    public ICollection<RecipeTimelineEvent> TimelineEvents { get; set; } = [];
    public ICollection<RecipeShareToken> ShareTokens { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Tool> Tools { get; set; } = [];
    public Group Group { get; set; } = null!;
    public Household Household { get; set; } = null!;
}
