using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IO.Common.Core.Email;
using IO.Common.Core;
using IO.Common.Models;
using IO.Platform.Common.Core.Routing;

namespace IO.Common.Routes;

/// <summary>
/// Static route definitions for Email API endpoints
/// </summary>
public static class EmailRoutes
{
    /// <summary>
    /// Maps all email-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapEmailRoutes(this IEndpointRouteBuilder routes)
    {
        // Group email routes with common prefix and authorization
        var emailGroup = routes.CreateApiGroup("email", "Email", "Email API");

        // Send email endpoint
        emailGroup.MapPost("send", async (
            [FromBody] EmailRequest request,
            IEmailBusinessService emailService,
            HttpContext context,
            ILogger<EmailRoutes> logger) =>
        {
            var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("UserId")?.Value;
            
            logger.LogInformation("User {UserId} sending email to {RecipientsCount} recipients", 
                userId, request.To?.Count ?? 0);

            var result = await emailService.SendEmailAsync(request, userId);

            return result.Success 
                ? Results.Ok(new { 
                    messageId = result.MessageId,
                    sentAt = result.SentAt
                })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("SendEmail")
        .Produces(200)
        .Produces(400);

        // Send template email endpoint
        emailGroup.MapPost("template/send", async (
            [FromBody] TemplateEmailRequest request,
            IEmailBusinessService emailService,
            HttpContext context,
            ILogger<EmailRoutes> logger) =>
        {
            var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("UserId")?.Value;
            
            logger.LogInformation("User {UserId} sending template email {TemplateId} to {RecipientsCount} recipients", 
                userId, request.TemplateId, request.To?.Count ?? 0);

            var result = await emailService.SendTemplateEmailAsync(request, userId);

            return result.Success 
                ? Results.Ok(new { 
                    messageId = result.MessageId,
                    sentAt = result.SentAt
                })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("SendTemplateEmail")
        .Produces(200)
        .Produces(400);

        // Get email history endpoint
        emailGroup.MapGet("history", async (
            [FromQuery] int limit = 50,
            IEmailBusinessService emailService,
            HttpContext context,
            ILogger<EmailRoutes> logger) =>
        {
            var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("UserId")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Results.BadRequest(new { error = "User ID not found" });
            }

            logger.LogInformation("Getting email history for user {UserId}", userId);

            var emails = await emailService.GetEmailHistoryAsync(userId, limit);

            return Results.Ok(new { 
                emails,
                count = emails.Count
            });
        })
        .WithName("GetEmailHistory")
        .Produces(200)
        .Produces(400);

        // Get email status endpoint
        emailGroup.MapGet("status/{messageId}", async (
            string messageId,
            IEmailBusinessService emailService,
            ILogger<EmailRoutes> logger) =>
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return Results.BadRequest(new { error = "Message ID is required" });
            }

            logger.LogInformation("Getting email status for message {MessageId}", messageId);

            var status = await emailService.GetEmailStatusAsync(messageId);

            return Results.Ok(status);
        })
        .WithName("GetEmailStatus")
        .Produces(200)
        .Produces(400);

        return routes;
    }
}
