using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace Mealie.Infrastructure.Auth;

public static class OidcConfiguration
{
    public static AuthenticationBuilder AddMealieOidc(
        this AuthenticationBuilder builder,
        AppSettings settings)
    {
        if (!settings.OidcEnabled || string.IsNullOrEmpty(settings.OidcAuthority))
            return builder;

        return builder.AddOpenIdConnect("oidc", options =>
        {
            options.Authority = settings.OidcAuthority;
            options.ClientId = settings.OidcClientId;
            options.ClientSecret = settings.OidcClientSecret;
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.CallbackPath = "/api/auth/oauth/callback";
        });
    }
}
