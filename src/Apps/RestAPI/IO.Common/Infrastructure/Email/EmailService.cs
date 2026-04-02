using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IO.Common.Infrastructure.Email;

/// <summary>
/// Simple email service for sending emails via SendGrid
/// </summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _sendGridApiKey;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _sendGridApiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey");
    }

    /// <summary>
    /// Send an email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML content of the email</param>
    /// <param name="textContent">Plain text content of the email</param>
    /// <returns>True if email was sent successfully</returns>
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress("noreply@usxpress.com", "USXpress");
            var toEmail = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, textContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}", to, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", to);
            return false;
        }
    }
}
