using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Auth;

public class RegistrationService(
    ApplicationDbContext db,
    IOptions<AppSettings> settings,
    IRegistrationInviteService registrationInviteService,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    public bool AllowSignup => settings.Value.AllowSignup;

    public async Task<RegistrationInvitePrefillResponse?> ResolveInviteAsync(string invite, CancellationToken ct = default)
    {
        var inviteContext = await ResolveInviteContextAsync(invite, ct);
        if (inviteContext is null)
        {
            return null;
        }

        return new RegistrationInvitePrefillResponse
        {
            Email = inviteContext.Value.Email,
        };
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (!AllowSignup)
        {
            logger.LogWarning("Registration attempted but ALLOW_SIGNUP is false");
            return new RegistrationResult(false, "Registration is disabled");
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        var exists = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Username == username || u.Email == email, ct);
        if (exists)
        {
            return new RegistrationResult(false, "Registration failed — username or email already taken");
        }

        Guid groupId;
        Guid? householdId = null;

        if (!string.IsNullOrWhiteSpace(request.Invite))
        {
            var inviteContext = await ResolveInviteContextAsync(request.Invite, ct);
            if (inviteContext is null)
            {
                return new RegistrationResult(false, "Registration invite is invalid or expired");
            }

            if (!string.Equals(email, inviteContext.Value.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new RegistrationResult(false, "Registration email must match the invitation email");
            }

            groupId = inviteContext.Value.Token.GroupId;
            householdId = inviteContext.Value.Token.HouseholdId;
        }
        else if (!string.IsNullOrWhiteSpace(request.GroupToken))
        {
            var token = await db.InviteTokens
                .FirstOrDefaultAsync(t => t.Token == request.GroupToken.Trim(), ct);
            if (token is null)
            {
                return new RegistrationResult(false, "Group token is invalid or expired");
            }

            groupId = token.GroupId;
            householdId = token.HouseholdId;
        }
        else
        {
            var defaultGroup = await db.Groups.FirstOrDefaultAsync(ct);
            if (defaultGroup is null)
            {
                return new RegistrationResult(false, "Registration is unavailable");
            }

            groupId = defaultGroup.Id;
            householdId = await db.Households
                .Where(h => h.GroupId == groupId)
                .Select(h => (Guid?)h.Id)
                .FirstOrDefaultAsync(ct);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = fullName,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AuthMethod = AuthMethod.Mealie,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("New user registered: {Username}", username);
        return new RegistrationResult(true, AuthContext: new RegistrationAuthContext(
            user.Id,
            user.GroupId,
            user.HouseholdId ?? Guid.Empty,
            user.Admin));
    }

    private async Task<(string Email, GroupInviteToken Token)?> ResolveInviteContextAsync(string invite,
        CancellationToken ct)
    {
        var payload = registrationInviteService.DecodeInvite(invite);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.GroupToken))
        {
            return null;
        }

        var token = await db.InviteTokens
            .FirstOrDefaultAsync(t => t.Token == payload.GroupToken.Trim(), ct);
        if (token is null)
        {
            return null;
        }

        return (payload.Email.Trim(), token);
    }
}
