namespace Mealie.Application.Dtos.Recipes;

public class AssetResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public string FileName => $"{Name}.{Extension}";
}
