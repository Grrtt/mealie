namespace Mealie.Infrastructure.Email;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendPasswordResetEmailAsync(string to, string resetUrl, CancellationToken ct = default);
    Task SendInvitationEmailAsync(string to, string groupName, string inviteUrl, CancellationToken ct = default);
}
