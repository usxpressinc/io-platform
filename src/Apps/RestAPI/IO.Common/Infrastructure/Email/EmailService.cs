using SendGrid;
using SendGrid.Helpers.Mail;

namespace IO.Common.Infrastructure.Email;

/// <summary>
/// Simple email service for sending emails via SendGrid
/// </summary>
public class EmailService(ILogger<EmailService> logger, IConfiguration configuration)
{
    private readonly string _sendGridApiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey");

    /// <summary>
    /// Send an email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML content of the email</param>
    /// <param name="textContent">Plain text content of the email</param>
    /// <returns>True if the email was sent successfully</returns>
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            var client = new SendGridClient(this._sendGridApiKey);
            var from = new EmailAddress("noreply@usxpress.com", "USXpress");
            var toEmail = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, textContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            else
            {
                logger.LogError("Failed to send email to {Email}. Status: {StatusCode}", to, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {Email}", to);
            return false;
        }
    }
}
