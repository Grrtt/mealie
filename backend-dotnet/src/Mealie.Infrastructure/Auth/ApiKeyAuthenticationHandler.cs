using System.Security.Claims;
using System.Text.Encodings.Web;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Infrastructure.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApplicationDbContext db)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private const string ApiKeyPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(ApiKeyPrefix))
        {
            return AuthenticateResult.NoResult();
        }

        var rawToken = authHeader[ApiKeyPrefix.Length..].Trim();

        var apiKeys = await db.ApiKeys
            .Include(k => k.User)
            .ToListAsync();

        var matched = apiKeys.FirstOrDefault(k => BCrypt.Net.BCrypt.Verify(rawToken, k.Token));
        if (matched is null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var user = matched.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new("group_id", user.GroupId.ToString()),
            new("household_id", user.HouseholdId?.ToString() ?? string.Empty),
            new("admin", user.Admin.ToString().ToLower())
        };
        if (user.Admin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "admin"));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
