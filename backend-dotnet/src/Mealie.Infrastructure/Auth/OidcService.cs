using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Infrastructure.Auth;

public class OidcUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? PreferredUsername { get; set; }
}

public interface IOidcService
{
    bool IsConfigured { get; }
    string? GetAuthorizationUrl(string redirectUri, string state, string nonce);
    Task<OidcUserInfo?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<User?> ProvisionUserAsync(OidcUserInfo userInfo, CancellationToken ct = default);
}

public class OidcService(
    IOptions<AppSettings> settings,
    IHttpClientFactory httpClientFactory,
    ApplicationDbContext db,
    ILogger<OidcService> logger) : IOidcService
{
    private readonly AppSettings _settings = settings.Value;
    private OidcDiscoveryDocument? _discoveryDoc;

    public bool IsConfigured =>
        _settings.OidcEnabled && !string.IsNullOrEmpty(_settings.OidcAuthority);

    public string? GetAuthorizationUrl(string redirectUri, string state, string nonce)
    {
        if (!IsConfigured || _settings.OidcAuthority is null)
        {
            return null;
        }

        // Use the well-known authorization endpoint path pattern (per OIDC spec)
        var authority = _settings.OidcAuthority.TrimEnd('/');
        return $"{authority}/protocol/openid-connect/auth" +
               $"?client_id={Uri.EscapeDataString(_settings.OidcClientId ?? "")}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope=openid+email+profile" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&nonce={Uri.EscapeDataString(nonce)}";
    }

    public async Task<OidcUserInfo?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            var discovery = await GetDiscoveryDocumentAsync(ct);
            if (discovery is null)
            {
                return null;
            }

            var client = httpClientFactory.CreateClient("oidc");

            // Exchange authorization code for tokens
            var tokenReq = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = _settings.OidcClientId ?? "",
                ["client_secret"] = _settings.OidcClientSecret ?? ""
            };

            var tokenResp = await client.PostAsync(discovery.TokenEndpoint, new FormUrlEncodedContent(tokenReq), ct);
            if (!tokenResp.IsSuccessStatusCode)
            {
                logger.LogWarning("OIDC token exchange failed: {Status}", tokenResp.StatusCode);
                return null;
            }

            var tokenData = await tokenResp.Content.ReadFromJsonAsync<OidcTokenResponse>(ct);
            if (tokenData?.AccessToken is null)
            {
                return null;
            }

            // Fetch user info
            using var userInfoReq = new HttpRequestMessage(HttpMethod.Get, discovery.UserInfoEndpoint);
            userInfoReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            var userInfoResp = await client.SendAsync(userInfoReq, ct);
            if (!userInfoResp.IsSuccessStatusCode)
            {
                logger.LogWarning("OIDC userinfo request failed: {Status}", userInfoResp.StatusCode);
                return null;
            }

            return await userInfoResp.Content.ReadFromJsonAsync<OidcUserInfo>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OIDC code exchange");
            return null;
        }
    }

    public async Task<User?> ProvisionUserAsync(OidcUserInfo userInfo, CancellationToken ct = default)
    {
        // Match by email (same strategy as Python's OIDC_USER_CLAIM = "email")
        if (userInfo.Email is not null)
        {
            var existing = await db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == userInfo.Email, ct);
            if (existing is not null)
            {
                return existing;
            }
        }

        // Auto-provision new user into the default group/household
        var defaultGroup = await db.Groups.FirstOrDefaultAsync(ct);
        if (defaultGroup is null)
        {
            return null;
        }

        var defaultHousehold = await db.Households
            .FirstOrDefaultAsync(h => h.GroupId == defaultGroup.Id, ct);

        var username = userInfo.PreferredUsername
                       ?? userInfo.Email?.Split('@')[0]
                       ?? userInfo.Sub;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = userInfo.Email ?? $"{username}@oidc.local",
            FullName = userInfo.Name,
            AuthMethod = AuthMethod.OIDC,
            GroupId = defaultGroup.Id,
            HouseholdId = defaultHousehold?.Id,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Auto-provisioned OIDC user {Username}", username);
        return user;
    }

    private async Task<OidcDiscoveryDocument?> GetDiscoveryDocumentAsync(CancellationToken ct)
    {
        if (_discoveryDoc is not null)
        {
            return _discoveryDoc;
        }

        try
        {
            var client = httpClientFactory.CreateClient("oidc");
            var url = $"{_settings.OidcAuthority!.TrimEnd('/')}/.well-known/openid-configuration";
            _discoveryDoc = await client.GetFromJsonAsync<OidcDiscoveryDocument>(url, ct);
            return _discoveryDoc;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch OIDC discovery document from {Authority}", _settings.OidcAuthority);
            return null;
        }
    }

    private class OidcTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }

        [JsonPropertyName("id_token")] public string? IdToken { get; set; }
    }

    private class OidcDiscoveryDocument
    {
        [JsonPropertyName("token_endpoint")] public string TokenEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;
    }
}
