using Mealie.Application.Dtos.Users;

namespace Mealie.Application.Services.Users;

public interface IUserService
{
    Task<UserResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserResponse?> UpdateProfileAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
    Task<ApiKeyResponse> CreateApiKeyAsync(Guid userId, string name, CancellationToken ct = default);
    Task<IList<ApiKeyResponse>> GetApiKeysAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteApiKeyAsync(Guid userId, int keyId, CancellationToken ct = default);
    Task AddFavoriteAsync(Guid userId, string slug, CancellationToken ct = default);
    Task RemoveFavoriteAsync(Guid userId, string slug, CancellationToken ct = default);
    Task<IList<UserRatingResponse>> GetRatingsAsync(Guid userId, CancellationToken ct = default);
    Task SetRatingAsync(Guid userId, string slug, int? rating, bool? isFavorite, CancellationToken ct = default);
}
