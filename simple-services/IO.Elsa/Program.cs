var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<IPricingService, PricingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
PricingEndpoints.Register(app);

app.Run();

// Services
public interface IPricingService
{
    Task<PricingResponse> CalculatePriceAsync(PricingRequest request);
    Task<HealthResponse> GetHealthAsync();
}

public class PricingService : IPricingService
{
    public Task<PricingResponse> CalculatePriceAsync(PricingRequest request)
    {
        return Task.FromResult(new PricingResponse
        {
            Service = "IO.Elsa",
            CalculatedPrice = 250.00,
            RateCardUsed = "Standard Rate Card",
            CustomerId = request.CustomerId,
            ServiceType = request.ServiceType,
            Distance = request.Distance,
            Weight = request.Weight,
            Volume = request.Volume,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Elsa",
            Description = "Pricing Service",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

// Static endpoint handlers
public static class PricingEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (IPricingService service) => await service.GetHealthAsync());

        app.MapGet("/", () => "IO.Elsa - Pricing Service is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Elsa",
            Description = "Pricing Service",
            Port = 8083,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/pricing/calculate" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapPost("/api/pricing/calculate", async (PricingRequest request, IPricingService service) => 
            await service.CalculatePriceAsync(request));
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

public record PricingResponse
{
    public string Service { get; init; } = string.Empty;
    public double CalculatedPrice { get; init; }
    public string RateCardUsed { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string ServiceType { get; init; } = string.Empty;
    public double Distance { get; init; }
    public double Weight { get; init; }
    public double Volume { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PricingRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public string ServiceType { get; init; } = string.Empty;
    public double Distance { get; init; }
    public double Weight { get; init; }
    public double Volume { get; init; }
}
