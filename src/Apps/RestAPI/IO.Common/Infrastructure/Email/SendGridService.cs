using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Common.Infrastructure.Email;

/// <summary>
/// SendGrid email service implementation for IO Platform
/// Provides email sending with HTML template rendering and signature support
/// </summary>
public interface IEmailService
{
    Task<EmailResult> SendEmailAsync(EmailRequest request);
    Task<EmailResult> SendTemplateEmailAsync(TemplateEmailRequest request);
    Task<EmailResult> SendBulkEmailAsync(BulkEmailRequest request);
    Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string messageId);
}

/// <summary>
/// SendGrid email service implementation
/// </summary>
public class SendGridService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<SendGridService> _logger;
    private readonly EmailSettings _settings;

    public SendGridService(
        IConfiguration configuration,
        ILogger<SendGridService> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("SendGrid:ApiKey", "SendGrid API key is required");
        }

        _sendGridClient = new SendGridClient(apiKey);
        _logger.LogInformation("SendGrid email service initialized");
    }

    public async Task<EmailResult> SendEmailAsync(EmailRequest request)
    {
        try
        {
            _logger.LogInformation("Sending email to {RecipientsCount} recipients", request.To.Count);

            var msg = BuildSendGridMessage(request);
            var response = await _sendGridClient.SendEmailAsync(msg);

            var result = new EmailResult
            {
                Success = response.IsSuccessStatusCode,
                MessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault(),
                StatusCode = response.StatusCode,
                ResponseBody = await response.Body.ReadAsStringAsync(),
                SentAt = DateTime.UtcNow
            };

            if (result.Success)
            {
                _logger.LogInformation("Email sent successfully. Message ID: {MessageId}", result.MessageId);
            }
            else
            {
                _logger.LogError("Failed to send email. Status: {StatusCode}, Body: {Body}", 
                    result.StatusCode, result.ResponseBody);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailResult> SendTemplateEmailAsync(TemplateEmailRequest request)
    {
        try
        {
            _logger.LogInformation("Sending template email {TemplateId} to {RecipientsCount} recipients", 
                request.TemplateId, request.To.Count);

            var msg = BuildSendGridTemplateMessage(request);
            var response = await _sendGridClient.SendEmailAsync(msg);

            var result = new EmailResult
            {
                Success = response.IsSuccessStatusCode,
                MessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault(),
                StatusCode = response.StatusCode,
                ResponseBody = await response.Body.ReadAsStringAsync(),
                SentAt = DateTime.UtcNow
            };

            if (result.Success)
            {
                _logger.LogInformation("Template email sent successfully. Message ID: {MessageId}", result.MessageId);
            }
            else
            {
                _logger.LogError("Failed to send template email. Status: {StatusCode}, Body: {Body}", 
                    result.StatusCode, result.ResponseBody);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template email");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailResult> SendBulkEmailAsync(BulkEmailRequest request)
    {
        try
        {
            _logger.LogInformation("Sending bulk email to {RecipientsCount} recipients", request.Recipients.Count);

            var results = new List<EmailResult>();
            var batchSize = _settings.BulkEmailBatchSize;

            for (int i = 0; i < request.Recipients.Count; i += batchSize)
            {
                var batch = request.Recipients.Skip(i).Take(batchSize).ToList();
                var batchRequest = new EmailRequest
                {
                    To = batch.Select(r => new EmailAddress { Email = r.Email, Name = r.Name }).ToList(),
                    Subject = request.Subject,
                    HtmlContent = request.HtmlContent,
                    TextContent = request.TextContent,
                    From = request.From,
                    ReplyTo = request.ReplyTo,
                    Attachments = request.Attachments
                };

                var batchResult = await SendEmailAsync(batchRequest);
                results.Add(batchResult);

                // Add delay between batches to avoid rate limiting
                if (i + batchSize < request.Recipients.Count)
                {
                    await Task.Delay(_settings.BulkEmailDelay);
                }
            }

            var overallSuccess = results.All(r => r.Success);
            var totalRecipients = request.Recipients.Count;
            var successfulRecipients = results.Count(r => r.Success);

            return new EmailResult
            {
                Success = overallSuccess,
                MessageId = overallSuccess ? results.First(r => r.Success).MessageId : null,
                StatusCode = overallSuccess ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.MultiStatus,
                ErrorMessage = overallSuccess ? null : $"Some emails failed: {successfulRecipients}/{totalRecipients} successful",
                SentAt = DateTime.UtcNow,
                BulkResults = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk email");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string messageId)
    {
        try
        {
            // SendGrid doesn't provide a direct delivery status API in the free tier
            // This would typically integrate with SendGrid Event Webhook for real-time status
            // For now, return a placeholder status
            
            return new EmailDeliveryStatus
            {
                MessageId = messageId,
                Status = EmailStatus.Sent,
                LastUpdated = DateTime.UtcNow,
                Events = new List<EmailEvent>
                {
                    new EmailEvent
                    {
                        Event = EmailEventType.Processed,
                        Timestamp = DateTime.UtcNow,
                        Message = "Email processed by SendGrid"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery status for message {MessageId}", messageId);
            return new EmailDeliveryStatus
            {
                MessageId = messageId,
                Status = EmailStatus.Unknown,
                ErrorMessage = ex.Message,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    private SendGridMessage BuildSendGridMessage(EmailRequest request)
    {
        var from = new EmailAddress(request.From.Email, request.From.Name);
        var to = request.To.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var msg = MailHelper.CreateSingleEmail(from, to, request.Subject, request.TextContent, request.HtmlContent);

        // Add reply-to if specified
        if (request.ReplyTo != null)
        {
            msg.ReplyTo = new EmailAddress(request.ReplyTo.Email, request.ReplyTo.Name);
        }

        // Add CC if specified
        if (request.Cc?.Any() == true)
        {
            msg.AddCcs(request.Cc.Select(cc => new EmailAddress(cc.Email, cc.Name)));
        }

        // Add BCC if specified
        if (request.Bcc?.Any() == true)
        {
            msg.AddBccs(request.Bcc.Select(bcc => new EmailAddress(bcc.Email, bcc.Name)));
        }

        // Add attachments if specified
        if (request.Attachments?.Any() == true)
        {
            foreach (var attachment in request.Attachments)
            {
                msg.AddAttachment(attachment.Content, attachment.FileName, attachment.MimeType);
            }
        }

        // Add custom headers
        if (request.CustomHeaders?.Any() == true)
        {
            foreach (var header in request.CustomHeaders)
            {
                msg.AddHeader(header.Key, header.Value);
            }
        }

        // Add tracking settings
        if (_settings.EnableClickTracking)
        {
            msg.SetClickTracking(true);
        }

        if (_settings.EnableOpenTracking)
        {
            msg.SetOpenTracking(true);
        }

        if (_settings.EnableSubscriptionTracking)
        {
            msg.SetSubscriptionTracking(true);
        }

        return msg;
    }

    private SendGridMessage BuildSendGridTemplateMessage(TemplateEmailRequest request)
    {
        var from = new EmailAddress(request.From.Email, request.From.Name);
        var to = request.To.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var msg = MailHelper.CreateSingleTemplateEmail(from, to, request.TemplateId, request.TemplateData);

        // Add reply-to if specified
        if (request.ReplyTo != null)
        {
            msg.ReplyTo = new EmailAddress(request.ReplyTo.Email, request.ReplyTo.Name);
        }

        // Add CC if specified
        if (request.Cc?.Any() == true)
        {
            msg.AddCcs(request.Cc.Select(cc => new EmailAddress(cc.Email, cc.Name)));
        }

        // Add BCC if specified
        if (request.Bcc?.Any() == true)
        {
            msg.AddBccs(request.Bcc.Select(bcc => new EmailAddress(bcc.Email, bcc.Name)));
        }

        return msg;
    }
}

/// <summary>
/// Email settings configuration
/// </summary>
public class EmailSettings
{
    public string DefaultFromName { get; set; } = "IO Platform";
    public string DefaultFromEmail { get; set; } = "noreply@io-platform.com";
    public int BulkEmailBatchSize { get; set; } = 100;
    public TimeSpan BulkEmailDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool EnableClickTracking { get; set; } = true;
    public bool EnableOpenTracking { get; set; } = true;
    public bool EnableSubscriptionTracking { get; set; } = false;
    public TimeSpan DeliveryStatusCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Email request
/// </summary>
public class EmailRequest
{
    public EmailAddress From { get; set; } = new();
    public List<EmailAddress> To { get; set; } = new();
    public List<EmailAddress>? Cc { get; set; }
    public List<EmailAddress>? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public EmailAddress? ReplyTo { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public Dictionary<string, string>? CustomHeaders { get; set; }
}

/// <summary>
/// Template email request
/// </summary>
public class TemplateEmailRequest
{
    public EmailAddress From { get; set; } = new();
    public List<EmailAddress> To { get; set; } = new();
    public List<EmailAddress>? Cc { get; set; }
    public List<EmailAddress>? Bcc { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public Dictionary<string, string> TemplateData { get; set; } = new();
    public EmailAddress? ReplyTo { get; set; }
}

/// <summary>
/// Bulk email request
/// </summary>
public class BulkEmailRequest
{
    public List<EmailAddress> Recipients { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public EmailAddress From { get; set; } = new();
    public EmailAddress? ReplyTo { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
}

/// <summary>
/// Email address
/// </summary>
public class EmailAddress
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Email result
/// </summary>
public class EmailResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
    public List<EmailResult>? BulkResults { get; set; }
}

/// <summary>
/// Email delivery status
/// </summary>
public class EmailDeliveryStatus
{
    public string MessageId { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? ErrorMessage { get; set; }
    public List<EmailEvent> Events { get; set; } = new();
}

/// <summary>
/// Email event
/// </summary>
public class EmailEvent
{
    public EmailEventType Event { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Email event types
/// </summary>
public enum EmailEventType
{
    Processed,
    Delivered,
    Opened,
    Clicked,
    Bounced,
    Dropped,
    SpamReport
}

/// <summary>
/// Email status
/// </summary>
public enum EmailStatus
{
    Sent,
    Delivered,
    Bounced,
    Dropped,
    Spam,
    Unknown
}
