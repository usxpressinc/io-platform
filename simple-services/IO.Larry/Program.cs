var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<IVendorService, VendorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
VendorEndpoints.Register(app);

app.Run();

// Services
public interface IVendorService
{
    Task<VendorResponse> CreateVendorAsync(VendorRequest request);
    Task<HealthResponse> GetHealthAsync();
}

public class VendorService : IVendorService
{
    public Task<VendorResponse> CreateVendorAsync(VendorRequest request)
    {
        return Task.FromResult(new VendorResponse
        {
            Service = "IO.Larry",
            VendorId = "vendor-" + DateTime.UtcNow.Ticks,
            Status = "Created",
            VendorCode = request.VendorCode,
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            Services = request.Services,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Larry",
            Description = "Vendor Management Service",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

// Static endpoint handlers
public static class VendorEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (IVendorService service) => await service.GetHealthAsync());

        app.MapGet("/", () => "IO.Larry - Vendor Management Service is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Larry",
            Description = "Vendor Management Service",
            Port = 8084,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/vendor", "/api/vendor/{vendorId}" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapPost("/api/vendor", async (VendorRequest request, IVendorService service) => 
            await service.CreateVendorAsync(request));
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

public record VendorResponse
{
    public string Service { get; init; } = string.Empty;
    public string VendorId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string VendorCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string[] Services { get; init; } = Array.Empty<string>();
    public DateTime Timestamp { get; init; }
}

public record VendorRequest
{
    public string VendorCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string[] Services { get; init; } = Array.Empty<string>();
}
