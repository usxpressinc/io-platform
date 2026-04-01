var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IUserContextService, UserContextService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
CommonEndpoints.Register(app);

app.Run();

// Services
public interface IEmailService
{
    Task<EmailResponse> SendEmailAsync(EmailRequest request);
    Task<HealthResponse> GetHealthAsync();
}

public interface IUserContextService
{
    Task<UserContextResponse> GetUserContextAsync(string userId);
    Task<HealthResponse> GetHealthAsync();
}

public class EmailService : IEmailService
{
    public Task<EmailResponse> SendEmailAsync(EmailRequest request)
    {
        return Task.FromResult(new EmailResponse
        {
            Message = "Email sent successfully!",
            Service = "IO.Common",
            EmailId = Guid.NewGuid().ToString(),
            Status = "Sent",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Common",
            Description = "Common Services",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

public class UserContextService : IUserContextService
{
    public Task<UserContextResponse> GetUserContextAsync(string userId)
    {
        return Task.FromResult(new UserContextResponse
        {
            UserId = userId,
            Service = "IO.Common",
            Context = "User context retrieved successfully",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Common",
            Description = "Common Services",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

// Static endpoint handlers
public static class CommonEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (IEmailService emailService, IUserContextService userContextService) => 
            await emailService.GetHealthAsync());

        app.MapGet("/", () => "IO.Common - Common Services is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Common",
            Description = "Common Services",
            Port = 8081,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/email/send", "/api/usercontext/{userId}" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapPost("/api/email/send", async (EmailRequest request, IEmailService service) => 
            await service.SendEmailAsync(request));

        app.MapGet("/api/usercontext/{userId}", async (string userId, IUserContextService service) => 
            await service.GetUserContextAsync(userId));
    }
}

// DTOs
public record HealthResponse
{
    public string Status { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Version { get; init; } = string.Empty;
}

public record EmailResponse
{
    public string Message { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string EmailId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record EmailRequest
{
    public string ToEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public record UserContextResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
