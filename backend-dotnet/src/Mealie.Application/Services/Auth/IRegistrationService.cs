using Mealie.Application.Dtos.Users;

namespace Mealie.Application.Services.Auth;

public interface IRegistrationService
{
    bool AllowSignup { get; }
    Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
