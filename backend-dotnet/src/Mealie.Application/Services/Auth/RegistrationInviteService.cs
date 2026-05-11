using System.Text.Json;
using Mealie.Infrastructure.Admin;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Auth;

public interface IRegistrationInviteService
{
    string CreateInvite(string email, string groupToken);
    RegistrationInvitePayload? DecodeInvite(string invite);
}

public sealed record RegistrationInvitePayload(string Email, string GroupToken);

public class RegistrationInviteService(
    IApiKeyEncryptionService encryptionService,
    ILogger<RegistrationInviteService> logger) : IRegistrationInviteService
{
    public string CreateInvite(string email, string groupToken)
    {
        var payload = new RegistrationInvitePayload(email.Trim(), groupToken.Trim());
        var invite = encryptionService.Encrypt(JsonSerializer.Serialize(payload));

        if (string.IsNullOrWhiteSpace(invite))
        {
            throw new InvalidOperationException("Failed to create registration invite");
        }

        return invite;
    }

    public RegistrationInvitePayload? DecodeInvite(string invite)
    {
        if (string.IsNullOrWhiteSpace(invite))
        {
            return null;
        }

        var json = encryptionService.Decrypt(invite);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RegistrationInvitePayload>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize registration invite payload");
            return null;
        }
    }
}
