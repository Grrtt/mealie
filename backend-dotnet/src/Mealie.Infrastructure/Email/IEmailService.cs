namespace Mealie.Infrastructure.Email;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string to, string resetUrl, CancellationToken ct = default);
    Task SendInvitationEmailAsync(string to, string groupName, string inviteUrl, CancellationToken ct = default);
    bool IsConfigured { get; }
}
