using Mealie.Application.Dtos.Users;

namespace Mealie.Application.Services.Auth;

public interface IRegistrationService
{
    bool AllowSignup { get; }
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<RegistrationInvitePrefillResponse?> ResolveInviteAsync(string invite, CancellationToken ct = default);
}

public record RegistrationAuthContext(Guid UserId, Guid GroupId, Guid HouseholdId, bool IsAdmin);

public record RegistrationResult(bool Success, string? Error = null, RegistrationAuthContext? AuthContext = null);
