using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Common.Core.Email;
using IO.Common.Core;
using IO.Common.Infrastructure.Email;
using IO.Common.Models;
using System.Threading.Tasks;

namespace IO.Common.Endpoints;

/// <summary>
/// Email endpoints for IO.Common service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailBusinessService _emailService;
    private readonly IContextService _contextService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailBusinessService emailService,
        IContextService contextService,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _contextService = contextService;
        _logger = logger;
    }

    /// <summary>
    /// Send an email
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("UserId")?.Value;
            
            _logger.LogInformation("User {UserId} sending email to {RecipientsCount} recipients", 
                userId, request.To?.Count ?? 0);

            var result = await _emailService.SendEmailAsync(request, userId);

            if (result.Success)
            {
                return Ok(new { 
                    success = true,
                    messageId = result.MessageId,
                    sentAt = result.SentAt
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error sending email");
            return BadRequest(new { 
                success = false,
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Send a template email
    /// </summary>
    [HttpPost("template/send")]
    public async Task<IActionResult> SendTemplateEmail([FromBody] TemplateEmailRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("UserId")?.Value;
            
            _logger.LogInformation("User {UserId} sending template email {TemplateId} to {RecipientsCount} recipients", 
                userId, request.TemplateId, request.To?.Count ?? 0);

            var result = await _emailService.SendTemplateEmailAsync(request, userId);

            if (result.Success)
            {
                return Ok(new { 
                    success = true,
                    messageId = result.MessageId,
                    sentAt = result.SentAt
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error sending template email");
            return BadRequest(new { 
                success = false,
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template email");
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get email history for the current user
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetEmailHistory([FromQuery] int limit = 50)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("UserId")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found" });
            }

            _logger.LogInformation("Getting email history for user {UserId}", userId);

            var emails = await _emailService.GetEmailHistoryAsync(userId, limit);

            return Ok(new { 
                emails,
                count = emails.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email history");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get email delivery status
    /// </summary>
    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetEmailStatus(string messageId)
    {
        try
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return BadRequest(new { error = "Message ID is required" });
            }

            _logger.LogInformation("Getting email status for message {MessageId}", messageId);

            var status = await _emailService.GetEmailStatusAsync(messageId);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email status for message {MessageId}", messageId);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get User Context (matches Python functionality)
    /// </summary>
    [HttpGet("context")]
    public async Task<IActionResult> GetUserContext([FromQuery] string? id = null, [FromQuery] string? number = null)
    {
        try
        {
            _logger.LogInformation("Getting user context for ID: {Id}, Number: {Number}", id, number);

            var context = await _contextService.GetContextAsync(id, number);

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get Driver Context for Genesys (matches Python functionality)
    /// </summary>
    [HttpGet("genesys/driver")]
    public async Task<IActionResult> GetGenesysDriverContext([FromQuery] string? id = null, [FromQuery] string? number = null)
    {
        try
        {
            _logger.LogInformation("Getting Genesys driver context for ID: {Id}, Number: {Number}", id, number);

            var context = await _contextService.GetGenesysDriverContextAsync(id, number);

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Genesys driver context");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Add User Context (matches Python functionality)
    /// </summary>
    [HttpPost("context")]
    public async Task<IActionResult> AddUserContext([FromBody] ContextRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Context request is required" });
            }

            _logger.LogInformation("Adding user context for ID: {Id}, Number: {Number}", request.Id, request.Number);

            var success = await _contextService.AddContextAsync(request);

            if (success)
            {
                return Ok(new { message = "Context added successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to add context" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user context");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }
}
