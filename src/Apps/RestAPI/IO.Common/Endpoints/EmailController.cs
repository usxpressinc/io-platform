using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Common.Infrastructure.Email;
using IO.Common.Core;
using IO.Common.Models;
using System.Threading.Tasks;

namespace IO.Common.Endpoints;

/// <summary>
/// Email endpoints for IO.Common service - matching Python implementation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly IContextService _contextService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        EmailService emailService,
        IContextService contextService,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _contextService = contextService;
        _logger = logger;
    }

    /// <summary>
    /// Send an email - matches Python /send endpoint exactly
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            _logger.LogInformation("Sending email to {Email}", request.to_emails);

            var result = await _emailService.SendEmailAsync(request);

            if (result.errors.Any())
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return StatusCode(500, new SendEmailResponse
            {
                status = "500",
                errors = new List<string> { "Internal server error" }
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
