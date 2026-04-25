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
