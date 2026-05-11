using Mealie.Application.Services.Auth;
using Mealie.Infrastructure.Admin;
using Microsoft.Extensions.Logging;

namespace Mealie.UnitTests.Auth;

public class RegistrationInviteServiceTests
{
    [Fact]
    public void CreateInvite_AndDecodeInvite_RoundTripsPayload()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var encryption = new ApiKeyEncryptionService(
            "test-secret-at-least-32-characters-long",
            loggerFactory.CreateLogger<ApiKeyEncryptionService>());
        var service = new RegistrationInviteService(
            encryption,
            loggerFactory.CreateLogger<RegistrationInviteService>());

        var invite = service.CreateInvite("invitee@example.com", "group-token");
        var payload = service.DecodeInvite(invite);

        Assert.NotNull(payload);
        Assert.DoesNotContain("invitee@example.com", invite);
        Assert.DoesNotContain("group-token", invite);
        Assert.Equal("invitee@example.com", payload!.Email);
        Assert.Equal("group-token", payload.GroupToken);
    }

    [Fact]
    public void DecodeInvite_InvalidPayload_ReturnsNull()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var encryption = new ApiKeyEncryptionService(
            "test-secret-at-least-32-characters-long",
            loggerFactory.CreateLogger<ApiKeyEncryptionService>());
        var service = new RegistrationInviteService(
            encryption,
            loggerFactory.CreateLogger<RegistrationInviteService>());

        var payload = service.DecodeInvite("not-a-valid-invite");

        Assert.Null(payload);
    }
}
