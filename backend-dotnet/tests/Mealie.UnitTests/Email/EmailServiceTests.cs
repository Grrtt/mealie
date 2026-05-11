using Mealie.Infrastructure.Email;

namespace Mealie.UnitTests.Email;

public class EmailServiceTests
{
    [Fact]
    public void BuildInvitationEmailBody_IncludesSecureLink()
    {
        const string groupName = "Family";
        const string inviteUrl = "http://localhost/register?invite=opaque-token";

        var body = EmailService.BuildInvitationEmailBody(groupName, inviteUrl);

        Assert.Contains(groupName, body);
        Assert.Contains(inviteUrl, body);
        Assert.DoesNotContain("test-token", body);
        Assert.Contains("secure invitation link", body);
    }
}
