namespace Mealie.Domain.Entities.Recipes;

public class RecipeTimelineEvent
{
    public Guid Id { get; set; }
    public string? Subject { get; set; }
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
    public string? Image { get; set; }
    public Guid RecipeId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
