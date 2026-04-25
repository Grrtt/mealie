namespace Mealie.Application.Dtos.Users;

public class UserResponse
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string AuthMethod { get; set; } = "Mealie";
    public bool Admin { get; set; }
    public bool Advanced { get; set; }
    public Guid GroupId { get; set; }
    public Guid? HouseholdId { get; set; }
    public bool CanManageHousehold { get; set; }
    public bool CanManage { get; set; }
    public bool CanInvite { get; set; }
    public bool CanOrganize { get; set; }
}

public class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
}

public class ApiKeyResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public class UserRatingResponse
{
    public Guid Id { get; set; }
    public int? Rating { get; set; }
    public bool IsFavorite { get; set; }
    public Guid RecipeId { get; set; }
    public string Slug { get; set; } = string.Empty;
}

public class SetRatingRequest
{
    public int? Rating { get; set; }
    public bool? IsFavorite { get; set; }
}
