using IO.Common.Infrastructure.Email;
using Microsoft.AspNetCore.Routing;

namespace IO.Common.Routes;

/// <summary>
/// Static routing class for Common API endpoints.
/// </summary>
public static class CommonRoutes
{
    /// <summary>
    /// Maps Common API routes v1.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder with mapped routes.</returns>
    public static RouteGroupBuilder MapCommonRoutesV1(this RouteGroupBuilder group)
    {
        // Health check endpoint
        group.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
            .WithName("HealthCheck")
            .WithDescription("Basic health check endpoint")
            .WithTags("Health");

        // Info endpoint
        group.MapGet("/info", () => new 
        { 
            Service = "IO Common API", 
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow 
        })
            .WithName("Info")
            .WithDescription("Service information endpoint")
            .WithTags("Info");

        // Example email endpoint
        group.MapPost("/email/send", async (EmailService emailService, EmailRequest request) =>
        {
            var result = await emailService.SendEmailAsync(request.To, request.Subject, request.Body);
            return Results.Ok(new { Success = result, Message = result ? "Email sent successfully" : "Failed to send email" });
        })
            .WithName("SendEmail")
            .WithDescription("Send an email")
            .WithTags("Email")
            .Accepts<EmailRequest>("application/json");

        return group;
    }
}

/// <summary>
/// Email request model.
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Email subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content.
    /// </summary>
    public string Body { get; set; } = string.Empty;
}
