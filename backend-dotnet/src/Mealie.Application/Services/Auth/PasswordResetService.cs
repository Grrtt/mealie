using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Auth;

public interface IPasswordResetService
{
    Task<string?> GenerateResetTokenAsync(string email, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}

public class PasswordResetService(
    ApplicationDbContext db,
    IJwtTokenService jwtService,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    public async Task<string?> GenerateResetTokenAsync(string email, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;

        var token = jwtService.GenerateAccessToken(user.Id, user.GroupId, user.HouseholdId ?? Guid.Empty, false);
        logger.LogInformation("Password reset token generated for {Email}", email);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var principal = jwtService.ValidateToken(token);
        if (principal is null) return false;

        var userIdStr = principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return false;

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.LoginAttempts = 0;
        user.LockedAt = null;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
