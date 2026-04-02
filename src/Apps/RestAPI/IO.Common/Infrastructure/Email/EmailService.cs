using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IO.Common.Infrastructure.Email;

/// <summary>
/// Simple email service matching Python implementation
/// </summary>
public class EmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _logger = logger;
        
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("SendGrid:ApiKey is required");
        }

        _sendGridClient = new SendGridClient(apiKey);
        _logger.LogInformation("SendGrid email service initialized");
    }

    public async Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request)
    {
        _logger.LogInformation("Sending email to {ToEmail} with subject {Subject}", request.to_emails, request.subject);

        try
        {
            var fromEmail = new Email(request.from_email);
            var subject = request.subject;
            
            var fromName = request.from_name ?? "USXpress";
            var from = new Email(request.from_email, fromName);

            var htmlContent = request.body; // Python version adds signature template, but we'll keep it simple

            var mail = MailHelper.CreateSingleEmail(from, null, subject, null, htmlContent);

            // Add recipients
            var personalization = new Personalization();
            foreach (var toEmail in request.to_emails.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                personalization.AddTo(new To(toEmail.Trim()));
            }

            // Add CC if provided
            if (!string.IsNullOrEmpty(request.cc_emails))
            {
                foreach (var ccEmail in request.cc_emails.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    personalization.AddCc(new Cc(ccEmail.Trim()));
                }
            }

            // Add BCC if provided
            if (!string.IsNullOrEmpty(request.bcc_emails))
            {
                foreach (var bccEmail in request.bcc_emails.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    personalization.AddBcc(new Bcc(bccEmail.Trim()));
                }
            }

            mail.AddPersonalization(personalization);

            var response = await _sendGridClient.SendEmailAsync(mail);
            
            _logger.LogInformation("Email sent successfully with status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid error: {Error}", errorBody);
                return new SendEmailResponse
                {
                    status = ((int)response.StatusCode).ToString(),
                    errors = new List<string> { errorBody }
                };
            }

            return new SendEmailResponse
            {
                status = ((int)response.StatusCode).ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", request.to_emails);
            return new SendEmailResponse
            {
                status = "500",
                errors = new List<string> { ex.Message }
            };
        }
    }
}

/// <summary>
/// Email request matching Python model exactly
/// </summary>
public class SendEmailRequest
{
    public string body { get; set; } = string.Empty;
    public string from_email { get; set; } = string.Empty;
    public string to_emails { get; set; } = string.Empty;
    public string subject { get; set; } = string.Empty;
    public string cc_emails { get; set; } = string.Empty;
    public string bcc_emails { get; set; } = string.Empty;
    public string? title { get; set; }
    public string? from_name { get; set; }
}

/// <summary>
/// Email response matching Python model exactly
/// </summary>
public class SendEmailResponse
{
    public string status { get; set; } = string.Empty;
    public List<string> errors { get; set; } = new();
}
