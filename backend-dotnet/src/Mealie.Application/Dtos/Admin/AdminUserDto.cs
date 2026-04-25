namespace Mealie.Application.Dtos.Admin;

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool Admin { get; set; }
    public bool Advanced { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
    public bool CanManageHousehold { get; set; }
    public bool CanManage { get; set; }
    public bool CanInvite { get; set; }
    public bool CanOrganize { get; set; }
    public int LoginAttempts { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateAdminUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Password { get; set; } = string.Empty;
    public bool Admin { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
}

public class UpdateAdminUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool? Admin { get; set; }
    public bool? Advanced { get; set; }
    public Guid? HouseholdId { get; set; }
    public bool? CanManageHousehold { get; set; }
    public bool? CanManage { get; set; }
    public bool? CanInvite { get; set; }
    public bool? CanOrganize { get; set; }
}
