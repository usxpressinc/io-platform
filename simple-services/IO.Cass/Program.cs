var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<ICarrierVettingService, CarrierVettingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
CarrierEndpoints.Register(app);

app.Run();

// Services
public interface ICarrierVettingService
{
    Task<CarrierVettingResponse> VetCarrierAsync(string dotNumber);
    Task<CarrierStatusResponse> GetCarrierStatusAsync(string dotNumber);
    Task<HealthResponse> GetHealthAsync();
}

public class CarrierVettingService : ICarrierVettingService
{
    public Task<CarrierVettingResponse> VetCarrierAsync(string dotNumber)
    {
        return Task.FromResult(new CarrierVettingResponse
        {
            DotNumber = dotNumber,
            Service = "IO.Cass",
            VettingStatus = "In Progress",
            HighwayApiStatus = "Connected",
            McLeodApiStatus = "Connected",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<CarrierStatusResponse> GetCarrierStatusAsync(string dotNumber)
    {
        return Task.FromResult(new CarrierStatusResponse
        {
            DotNumber = dotNumber,
            Service = "IO.Cass",
            VettingStatus = "Approved",
            SafetyRating = "Satisfactory",
            InsuranceStatus = "Valid",
            AuthorityStatus = "Active",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Cass",
            Description = "Carrier Vetting Service",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

// Static endpoint handlers
public static class CarrierEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (ICarrierVettingService service) => await service.GetHealthAsync());

        app.MapGet("/", () => "IO.Cass - Carrier Vetting Service is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Cass",
            Description = "Carrier Vetting Service",
            Port = 8082,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/carrier/{dotNumber}/vet", "/api/carrier/{dotNumber}/status" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapGet("/api/carrier/{dotNumber}/vet", async (string dotNumber, ICarrierVettingService service) => 
            await service.VetCarrierAsync(dotNumber));

        app.MapGet("/api/carrier/{dotNumber}/status", async (string dotNumber, ICarrierVettingService service) => 
            await service.GetCarrierStatusAsync(dotNumber));
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

public record CarrierVettingResponse
{
    public string DotNumber { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string VettingStatus { get; init; } = string.Empty;
    public string HighwayApiStatus { get; init; } = string.Empty;
    public string McLeodApiStatus { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record CarrierStatusResponse
{
    public string DotNumber { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string VettingStatus { get; init; } = string.Empty;
    public string SafetyRating { get; init; } = string.Empty;
    public string InsuranceStatus { get; init; } = string.Empty;
    public string AuthorityStatus { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
