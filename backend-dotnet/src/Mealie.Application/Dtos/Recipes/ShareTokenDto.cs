namespace Mealie.Application.Dtos.Recipes;

public class ShareTokenResponse
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateShareTokenRequest
{
    public DateTime? ExpiresAt { get; set; }
}
