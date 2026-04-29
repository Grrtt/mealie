namespace Mealie.Application.Dtos.Groups;

public class GroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
}

public class HouseholdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid GroupId { get; set; }
}

public class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateHouseholdRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateHouseholdRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateInviteTokenRequest
{
    public Guid? HouseholdId { get; set; }
}

public class InviteTokenResponse
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
}

public class HouseholdStatisticsResponse
{
    public int TotalRecipes { get; set; }
    public int TotalUsers { get; set; }
    public int TotalCategories { get; set; }
    public int TotalTags { get; set; }
    public int TotalTools { get; set; }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class GroupPreferencesResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public bool PrivateGroup { get; set; }
    public string? FirstDayOfWeek { get; set; }
}

public class UpdateGroupPreferencesRequest
{
    public bool? PrivateGroup { get; set; }
    public string? FirstDayOfWeek { get; set; }
}

public class HouseholdPreferencesResponse
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public bool PrivateHousehold { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? RecipePublic { get; set; }
    public string? RecipeShowNutrition { get; set; }
    public string? RecipeShowAssets { get; set; }
    public string? RecipeLandscapeView { get; set; }
    public string? RecipeDisableComments { get; set; }
    public string? RecipeDisableAmount { get; set; }
}

public class UpdateHouseholdPreferencesRequest
{
    public bool? PrivateHousehold { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? RecipePublic { get; set; }
    public string? RecipeShowNutrition { get; set; }
    public string? RecipeShowAssets { get; set; }
    public string? RecipeLandscapeView { get; set; }
    public string? RecipeDisableComments { get; set; }
    public string? RecipeDisableAmount { get; set; }
}

public class GroupStorageResponse
{
    public long TotalSize { get; set; }
}

public class HouseholdMemberPermissions
{
    public Guid UserId { get; set; }
    public bool Admin { get; set; }
    public bool CanOrganize { get; set; }
    public bool CanInvite { get; set; }
}

public class HouseholdInvitationEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
