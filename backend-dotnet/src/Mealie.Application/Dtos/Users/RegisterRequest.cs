namespace Mealie.Application.Dtos.Users;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? GroupToken { get; set; }
    public string? Invite { get; set; }
}

public class RegistrationInvitePrefillResponse
{
    public string Email { get; set; } = string.Empty;
}
