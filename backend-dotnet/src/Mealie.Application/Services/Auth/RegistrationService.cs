using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Auth;

public class RegistrationService(
    ApplicationDbContext db,
    IOptions<AppSettings> settings,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    public bool AllowSignup => settings.Value.AllowSignup;

    public async Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (!AllowSignup)
        {
            logger.LogWarning("Registration attempted but ALLOW_SIGNUP is false");
            return false;
        }

        var exists = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email, ct);
        if (exists) return false;

        Guid groupId;
        Guid? householdId = null;

        if (!string.IsNullOrEmpty(request.GroupToken))
        {
            var token = await db.InviteTokens
                .FirstOrDefaultAsync(t => t.Token == request.GroupToken, ct);
            if (token is null) return false;
            groupId = token.GroupId;
            householdId = token.HouseholdId;
        }
        else
        {
            var defaultGroup = await db.Groups.FirstOrDefaultAsync(cancellationToken: ct);
            if (defaultGroup is null) return false;
            groupId = defaultGroup.Id;
            householdId = await db.Households
                .Where(h => h.GroupId == groupId)
                .Select(h => (Guid?)h.Id)
                .FirstOrDefaultAsync(ct);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AuthMethod = AuthMethod.Mealie,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("New user registered: {Username}", request.Username);
        return true;
    }
}
