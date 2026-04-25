using Mealie.Domain.Entities.Recipes;

namespace Mealie.Domain.Entities.Core;

public class User
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public AuthMethod AuthMethod { get; set; }
    public bool Admin { get; set; }
    public bool Advanced { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
    public string? CacheKey { get; set; }
    public int LoginAttempts { get; set; }
    public DateTime? LockedAt { get; set; }
    public bool CanManageHousehold { get; set; }
    public bool CanManage { get; set; }
    public bool CanInvite { get; set; }
    public bool CanOrganize { get; set; }
    public bool ShowAnnouncements { get; set; }
    public string? LastReadAnnouncement { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Group Group { get; set; } = null!;
    public Household? Household { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<RecipeComment> Comments { get; set; } = [];
    public ICollection<Recipe> FavoriteRecipes { get; set; } = [];
}
