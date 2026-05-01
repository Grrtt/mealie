using MailKit.Net.Smtp;
using MailKit.Security;
using Mealie.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace Mealie.Infrastructure.Email;

public class EmailService(AppSettings settings, ILogger<EmailService> logger) : IEmailService
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.SmtpHost);

    public async Task SendPasswordResetEmailAsync(string to, string resetUrl, CancellationToken ct = default)
    {
        var subject = "Mealie - Password Reset";
        var body =
            $"<p>You requested a password reset. Click the link below to reset your password:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p><p>If you did not request this, please ignore this email.</p>";
        await SendEmailAsync(to, subject, body, ct);
    }

    public async Task SendInvitationEmailAsync(string to, string groupName, string inviteUrl,
        CancellationToken ct = default)
    {
        var subject = $"Mealie - Invitation to join {groupName}";
        var body =
            $"<p>You have been invited to join the group <strong>{groupName}</strong> on Mealie.</p><p><a href=\"{inviteUrl}\">Click here to accept the invitation</a></p>";
        await SendEmailAsync(to, subject, body, ct);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured (SMTP_HOST is empty). Skipping email to {To}", to);
            return;
        }

        try
        {
            var message = new MimeMessage();
            var fromEmail = settings.SmtpFromEmail ?? settings.SmtpUser ?? "noreply@mealie.io";
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrWhiteSpace(settings.SmtpUser) && !string.IsNullOrWhiteSpace(settings.SmtpPassword))
            {
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
