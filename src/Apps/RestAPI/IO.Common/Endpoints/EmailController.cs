using Microsoft.AspNetCore.Mvc;
using IO.Common.Infrastructure.Email;
using IO.Common.Models;

namespace IO.Common.Endpoints;

/// <summary>
/// Email endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(EmailService emailService, ILogger<EmailController> logger)
    {
        this._emailService = emailService;
        this._logger = logger;
    }

    /// <summary>
    /// Send an email
    /// </summary>
    /// <param name="request">Email request details</param>
    /// <returns>Success status</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            this._logger.LogInformation("Sending email to {Email}", request.To);

            var result = await this._emailService.SendEmailAsync(request.To, request.Subject, request.HtmlContent, request.TextContent);

            if (result)
            {
                return this.Ok(new { success = true, message = "Email sent successfully" });
            }
            else
            {
                return this.BadRequest(new { success = false, message = "Failed to send email" });
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error sending email to {Email}", request.To);
            return this.StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}
