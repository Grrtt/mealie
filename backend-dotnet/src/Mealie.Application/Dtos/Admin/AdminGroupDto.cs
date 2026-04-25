namespace Mealie.Application.Dtos.Admin;

public class GroupPreferencesDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public bool PrivateGroup { get; set; }
    public bool ShowAnnouncements { get; set; }
}

public class UpdateGroupPreferencesDto
{
    public bool? PrivateGroup { get; set; }
    public bool? ShowAnnouncements { get; set; }
}

public class HouseholdPreferencesDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public bool PrivateHousehold { get; set; }
    public bool ShowAnnouncements { get; set; }
    public bool RecipePublic { get; set; }
    public bool RecipeShowNutrition { get; set; }
    public bool RecipeShowAssets { get; set; }
    public bool RecipeLandscapeView { get; set; }
    public bool RecipeDisableComments { get; set; }
    public bool RecipeDisableAmount { get; set; }
}

public class UpdateHouseholdPreferencesDto
{
    public bool? PrivateHousehold { get; set; }
    public bool? RecipePublic { get; set; }
    public bool? RecipeShowNutrition { get; set; }
    public bool? RecipeShowAssets { get; set; }
    public bool? RecipeLandscapeView { get; set; }
    public bool? RecipeDisableComments { get; set; }
    public bool? RecipeDisableAmount { get; set; }
}

public class AdminGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public int UserCount { get; set; }
    public int HouseholdCount { get; set; }
    public GroupPreferencesDto? Preferences { get; set; }
    public List<object> Users { get; set; } = [];
    public List<object> Households { get; set; } = [];
}

public class CreateAdminGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateAdminGroupRequest
{
    public string? Name { get; set; }
    public UpdateGroupPreferencesDto? Preferences { get; set; }
}

public class AdminHouseholdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public int UserCount { get; set; }
    public HouseholdPreferencesDto? Preferences { get; set; }
    public List<object> Users { get; set; } = [];
    public List<object> Webhooks { get; set; } = [];
}

public class CreateAdminHouseholdRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
}

public class UpdateAdminHouseholdRequest
{
    public string? Name { get; set; }
    public UpdateHouseholdPreferencesDto? Preferences { get; set; }
}
