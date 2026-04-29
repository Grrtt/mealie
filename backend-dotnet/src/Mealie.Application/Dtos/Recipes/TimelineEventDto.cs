namespace Mealie.Application.Dtos.Recipes;

public class TimelineEventResponse
{
    public Guid Id { get; set; }
    public string? Subject { get; set; }
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
    public Guid RecipeId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateTimelineEventRequest
{
    public string? RecipeSlug { get; set; }
    public string? Subject { get; set; }
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class UpdateTimelineEventRequest
{
    public string? Subject { get; set; }
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
}
