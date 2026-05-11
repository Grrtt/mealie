using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Mealie.Infrastructure.Auth;

public interface IAuthCookieService
{
    void SetAccessTokenCookie(HttpResponse response, string token);
    void ClearAccessTokenCookie(HttpResponse response);
}

public class AuthCookieService(IOptions<AppSettings> settings) : IAuthCookieService
{
    public const string AccessTokenCookieName = "mealie.access_token";

    public void SetAccessTokenCookie(HttpResponse response, string token)
    {
        response.Cookies.Append(AccessTokenCookieName, token, BuildCookieOptions(DateTimeOffset.UtcNow.AddHours(48)));
    }

    public void ClearAccessTokenCookie(HttpResponse response)
    {
        response.Cookies.Delete(AccessTokenCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            Expires = expires,
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = UsesHttps()
        };
    }

    private bool UsesHttps()
    {
        return Uri.TryCreate(settings.Value.BaseUrl, UriKind.Absolute, out var uri)
               && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
