using Mealie.Application.Dtos.Auth;

namespace Mealie.Application.Services.Auth;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(string username, string password);
    Task<TokenResponse?> RefreshAsync(string refreshToken);
    Task LockAccountAsync(Guid userId);
    Task UnlockAccountAsync(Guid userId);
}
