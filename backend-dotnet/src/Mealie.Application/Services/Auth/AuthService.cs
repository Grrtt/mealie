using Mealie.Application.Dtos.Auth;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Auth;

public class AuthService(
    ApplicationDbContext db,
    IJwtTokenService jwtService,
    ILdapAuthService ldapAuthService,
    ILogger<AuthService> logger) : IAuthService
{
    private const int MaxLoginAttempts = 5;

    public async Task<TokenResponse?> LoginAsync(string username, string password)
    {
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

        if (user is null)
        {
            logger.LogWarning("Login failed: user not found for {Username}", username);
            return null;
        }

        if (user.LockedAt is not null)
        {
            logger.LogWarning("Login failed: account locked for {Username}", username);
            return null;
        }

        // Route to the appropriate auth strategy based on the user's registered method
        if (user.AuthMethod == AuthMethod.LDAP)
        {
            var ldapOk = await ldapAuthService.AuthenticateAsync(username, password);
            if (!ldapOk)
            {
                logger.LogWarning("LDAP login failed for {Username}", username);
                return null;
            }
        }
        else
        {
            if (user.Password is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                user.LoginAttempts++;
                if (user.LoginAttempts >= MaxLoginAttempts)
                {
                    user.LockedAt = DateTime.UtcNow;
                    logger.LogWarning("Account locked after {Attempts} failed attempts for {Username}",
                        user.LoginAttempts, username);
                }

                await db.SaveChangesAsync();
                return null;
            }
        }

        // Successful login — reset attempts
        user.LoginAttempts = 0;
        user.LockedAt = null;
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(
            user.Id, user.GroupId, user.HouseholdId ?? Guid.Empty, user.Admin);

        return new TokenResponse { AccessToken = token, TokenType = "bearer" };
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        var principal = jwtService.ValidateToken(refreshToken);
        if (principal is null)
        {
            return null;
        }

        var userIdStr = principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return null;
        }

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        var token = jwtService.GenerateAccessToken(
            user.Id, user.GroupId, user.HouseholdId ?? Guid.Empty, user.Admin);
        return new TokenResponse { AccessToken = token, TokenType = "bearer" };
    }

    public async Task LockAccountAsync(Guid userId)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }

        user.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task UnlockAccountAsync(Guid userId)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }

        user.LockedAt = null;
        user.LoginAttempts = 0;
        await db.SaveChangesAsync();
    }
}
