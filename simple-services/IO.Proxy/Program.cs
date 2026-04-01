var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<IGatewayService, GatewayService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
GatewayEndpoints.Register(app);

app.Run();

// Services
public interface IGatewayService
{
    Task<HealthResponse> GetHealthAsync();
    Task<ApiResponse> RouteRequestAsync(string path);
}

public class GatewayService : IGatewayService
{
    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Proxy",
            Description = "API Gateway",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    public Task<ApiResponse> RouteRequestAsync(string path)
    {
        return Task.FromResult(new ApiResponse
        {
            Message = "Request routed successfully",
            Path = path,
            Service = "IO.Proxy",
            Timestamp = DateTime.UtcNow
        });
    }
}

// Static endpoint handlers
public static class GatewayEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (IGatewayService service) => await service.GetHealthAsync());

        app.MapGet("/", () => "IO.Proxy - API Gateway is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Proxy",
            Description = "API Gateway",
            Port = 8080,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/route" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapGet("/api/route/{*path}", async (string path, IGatewayService service) => 
            await service.RouteRequestAsync(path));
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

public record ApiResponse
{
    public string Message { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
