namespace Mealie.Application.Dtos.Admin;

public class AdminGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public int UserCount { get; set; }
    public int HouseholdCount { get; set; }
}

public class CreateAdminGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateAdminGroupRequest
{
    public string? Name { get; set; }
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
}

public class CreateAdminHouseholdRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
}

public class UpdateAdminHouseholdRequest
{
    public string? Name { get; set; }
}
