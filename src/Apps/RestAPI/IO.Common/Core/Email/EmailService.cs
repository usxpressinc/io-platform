using Microsoft.Extensions.Logging;
using IO.Common.Infrastructure.Email;
using IO.Common.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Core.Email;

/// <summary>
/// Email business logic service
/// </summary>
public interface IEmailBusinessService
{
    Task<EmailResult> SendEmailAsync(EmailRequest request, string? userId = null);
    Task<EmailResult> SendTemplateEmailAsync(TemplateEmailRequest request, string? userId = null);
    Task<List<EmailLog>> GetEmailHistoryAsync(string userId, int limit = 50);
    Task<EmailDeliveryStatus> GetEmailStatusAsync(string messageId);
}

/// <summary>
/// Email business logic implementation
/// </summary>
public class EmailBusinessService : IEmailBusinessService
{
    private readonly IEmailService _emailService;
    private readonly EmailLogRepository _emailLogRepository;
    private readonly ILogger<EmailBusinessService> _logger;

    public EmailBusinessService(
        IEmailService emailService,
        EmailLogRepository emailLogRepository,
        ILogger<EmailBusinessService> logger)
    {
        _emailService = emailService;
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }

    public async Task<EmailResult> SendEmailAsync(EmailRequest request, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Sending email business request to {RecipientsCount} recipients", request.To.Count);

            // Validate request
            ValidateEmailRequest(request);

            // Send email
            var result = await _emailService.SendEmailAsync(request);

            // Log to database
            var emailLog = new EmailLog
            {
                MessageId = result.MessageId ?? Guid.NewGuid().ToString(),
                FromEmail = request.From.Email,
                FromName = request.From.Name,
                ToEmail = string.Join(",", request.To.Select(t => t.Email)),
                ToName = string.Join(",", request.To.Select(t => t.Name)),
                Subject = request.Subject,
                Status = result.Success ? EmailStatus.Sent : EmailStatus.Dropped,
                ErrorMessage = result.ErrorMessage,
                SentAt = result.SentAt,
                Metadata = new Dictionary<string, object>
                {
                    ["UserId"] = userId ?? string.Empty,
                    ["RecipientsCount"] = request.To.Count,
                    ["HasAttachments"] = request.Attachments?.Any() == true,
                    ["IsTemplate"] = false
                }
            };

            await _emailLogRepository.CreateAsync(emailLog);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in email business service");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailResult> SendTemplateEmailAsync(TemplateEmailRequest request, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Sending template email {TemplateId} to {RecipientsCount} recipients", 
                request.TemplateId, request.To.Count);

            // Validate request
            ValidateTemplateEmailRequest(request);

            // Send template email
            var result = await _emailService.SendTemplateEmailAsync(request);

            // Log to database
            var emailLog = new EmailLog
            {
                MessageId = result.MessageId ?? Guid.NewGuid().ToString(),
                FromEmail = request.From.Email,
                FromName = request.From.Name,
                ToEmail = string.Join(",", request.To.Select(t => t.Email)),
                ToName = string.Join(",", request.To.Select(t => t.Name)),
                Subject = $"Template: {request.TemplateId}",
                TemplateId = request.TemplateId,
                Status = result.Success ? EmailStatus.Sent : EmailStatus.Dropped,
                ErrorMessage = result.ErrorMessage,
                SentAt = result.SentAt,
                Metadata = new Dictionary<string, object>
                {
                    ["UserId"] = userId ?? string.Empty,
                    ["RecipientsCount"] = request.To.Count,
                    ["TemplateData"] = request.TemplateData,
                    ["IsTemplate"] = true
                }
            };

            await _emailLogRepository.CreateAsync(emailLog);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in template email business service");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<EmailLog>> GetEmailHistoryAsync(string userId, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Getting email history for user {UserId}", userId);

            var emailLogs = await _emailLogRepository.GetByEmailAsync(userId, limit);
            
            _logger.LogInformation("Found {EmailCount} emails for user {UserId}", emailLogs.Count, userId);
            
            return emailLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email history for user {UserId}", userId);
            return new List<EmailLog>();
        }
    }

    public async Task<EmailDeliveryStatus> GetEmailStatusAsync(string messageId)
    {
        try
        {
            _logger.LogInformation("Getting email status for message {MessageId}", messageId);

            var status = await _emailService.GetDeliveryStatusAsync(messageId);
            
            // Update log if status changed
            var emailLog = await _emailLogRepository.GetByIdAsync(messageId);
            if (emailLog != null && emailLog.Status != status.Status)
            {
                await _emailLogRepository.UpdateStatusAsync(messageId, status.Status, status.ErrorMessage);
            }
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email status for message {MessageId}", messageId);
            return new EmailDeliveryStatus
            {
                MessageId = messageId,
                Status = EmailStatus.Unknown,
                ErrorMessage = ex.Message,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    private void ValidateEmailRequest(EmailRequest request)
    {
        if (request.To == null || !request.To.Any())
        {
            throw new ValidationException("At least one recipient is required");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ValidationException("Subject is required");
        }

        if (string.IsNullOrWhiteSpace(request.HtmlContent) && string.IsNullOrWhiteSpace(request.TextContent))
        {
            throw new ValidationException("Either HTML content or text content is required");
        }

        // Validate email addresses
        foreach (var recipient in request.To)
        {
            if (!IsValidEmail(recipient.Email))
            {
                throw new ValidationException($"Invalid email address: {recipient.Email}");
            }
        }

        if (!IsValidEmail(request.From.Email))
        {
            throw new ValidationException($"Invalid sender email: {request.From.Email}");
        }
    }

    private void ValidateTemplateEmailRequest(TemplateEmailRequest request)
    {
        if (request.To == null || !request.To.Any())
        {
            throw new ValidationException("At least one recipient is required");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            throw new ValidationException("Template ID is required");
        }

        // Validate email addresses
        foreach (var recipient in request.To)
        {
            if (!IsValidEmail(recipient.Email))
            {
                throw new ValidationException($"Invalid email address: {recipient.Email}");
            }
        }

        if (!IsValidEmail(request.From.Email))
        {
            throw new ValidationException($"Invalid sender email: {request.From.Email}");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
