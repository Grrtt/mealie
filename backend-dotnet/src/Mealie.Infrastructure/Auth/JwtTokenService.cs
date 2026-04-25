using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mealie.Infrastructure.Configuration;

namespace Mealie.Infrastructure.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid groupId, Guid householdId, bool isAdmin);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtTokenService(IOptions<AppSettings> settings) : IJwtTokenService
{
    private readonly AppSettings _settings = settings.Value;

    public string GenerateAccessToken(Guid userId, Guid groupId, Guid householdId, bool isAdmin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new("group_id", groupId.ToString()),
            new("household_id", householdId.ToString()),
            new("admin", isAdmin.ToString().ToLower()),
        };
        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "admin"));

        var token = new JwtSecurityToken(
            issuer: "mealie",
            audience: "mealie",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(48),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray());

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "mealie",
                    ValidateAudience = true,
                    ValidAudience = "mealie",
                    ClockSkew = TimeSpan.Zero
                }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
