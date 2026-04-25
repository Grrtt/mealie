using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace Mealie.Infrastructure.Auth;

public interface ILdapAuthService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}

public class LdapAuthService(
    IOptions<AppSettings> settings,
    ApplicationDbContext db,
    ILogger<LdapAuthService> logger) : ILdapAuthService
{
    private readonly AppSettings _settings = settings.Value;

    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        if (!_settings.LdapEnabled || string.IsNullOrEmpty(_settings.LdapServer))
            return false;

        try
        {
            using var conn = new LdapConnection { SecureSocketLayer = false };
            conn.ConnectionTimeout = _settings.LdapQueryTimeout * 1000;
            conn.Connect(_settings.LdapServer, _settings.LdapPort);

            var bindDn = _settings.LdapBindTemplate?.Replace("{username}", username) ?? username;
            conn.Bind(bindDn, password);

            if (!conn.Bound) return false;

            await ProvisionUserIfNeededAsync(username, ct);
            return true;
        }
        catch (LdapException ex) when (ex.ResultCode == LdapException.InvalidCredentials)
        {
            logger.LogInformation("LDAP authentication failed for {Username}", username);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LDAP server unavailable for {Username}", username);
            return false;
        }
    }

    private async Task ProvisionUserIfNeededAsync(string username, CancellationToken ct)
    {
        var existing = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Username == username, ct);
        if (existing) return;

        var defaultGroup = await db.Groups.FirstOrDefaultAsync(cancellationToken: ct);
        if (defaultGroup is null) return;

        var defaultHousehold = await db.Households.FirstOrDefaultAsync(h => h.GroupId == defaultGroup.Id, ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@ldap.local",
            AuthMethod = AuthMethod.LDAP,
            GroupId = defaultGroup.Id,
            HouseholdId = defaultHousehold?.Id,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Auto-provisioned LDAP user {Username}", username);
    }
}
