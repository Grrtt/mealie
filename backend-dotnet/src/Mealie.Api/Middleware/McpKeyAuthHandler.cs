using System.Security.Claims;
using System.Text.Encodings.Web;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Middleware;

public class McpKeyAuthOptions : AuthenticationSchemeOptions { }

public class McpKeyAuthHandler(
    IOptionsMonitor<McpKeyAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppSettings appSettings)
    : AuthenticationHandler<McpKeyAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(appSettings.McpSecret))
            return Task.FromResult(AuthenticateResult.Fail("MCP is not configured (McpSecret is not set)."));

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var header = authHeader.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = header["Bearer ".Length..].Trim();

        if (token != appSettings.McpSecret)
            return Task.FromResult(AuthenticateResult.Fail("Invalid MCP secret."));

        var claims = new[] { new Claim(ClaimTypes.Name, "mcp-client") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
