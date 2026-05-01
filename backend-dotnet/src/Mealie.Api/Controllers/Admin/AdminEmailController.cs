using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/email")]
[Authorize(Roles = "admin")]
public class AdminEmailController(IEmailService emailService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendTestEmail([FromBody] EmailTestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new EmailTestResponse
            {
                Success = false,
                Message = "Email address is required"
            });
        }

        if (!emailService.IsConfigured)
        {
            return StatusCode(424, new EmailTestResponse
            {
                Success = false,
                Message = "Email service is not configured (SMTP_HOST is not set)"
            });
        }

        try
        {
            const string testResetUrl = "http://localhost:3000/reset-password?token=test-token";
            await emailService.SendPasswordResetEmailAsync(request.Email, testResetUrl, ct);

            return Ok(new EmailTestResponse
            {
                Success = true,
                Message = $"Test email sent to {request.Email}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new EmailTestResponse
            {
                Success = false,
                Message = $"Failed to send test email: {ex.Message}"
            });
        }
    }
}
