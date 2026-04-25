using Mealie.Application.Dtos.Admin;

namespace Mealie.Application.Services.Admin;

public interface IAdminUserService
{
    Task<IList<AdminUserResponse>> GetAllUsersAsync(CancellationToken ct = default);
    Task<AdminUserResponse?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<AdminUserResponse?> CreateUserAsync(CreateAdminUserRequest request, CancellationToken ct = default);
    Task<AdminUserResponse?> UpdateUserAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken ct = default);
}
