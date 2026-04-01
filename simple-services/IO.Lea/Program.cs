var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddSingleton<IJobService, JobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure endpoints
JobEndpoints.Register(app);

app.Run();

// Services
public interface IJobService
{
    Task<JobResponse> CreateJobAsync(JobRequest request);
    Task<HealthResponse> GetHealthAsync();
}

public class JobService : IJobService
{
    public Task<JobResponse> CreateJobAsync(JobRequest request)
    {
        return Task.FromResult(new JobResponse
        {
            Service = "IO.Lea",
            JobId = "job-" + DateTime.UtcNow.Ticks,
            Status = "Created",
            CustomerId = request.CustomerId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            EmploymentType = request.EmploymentType,
            CompanyName = request.CompanyName,
            ApplicationUrl = request.ApplicationUrl,
            GoogleJobsPosted = true,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<HealthResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "healthy",
            Service = "IO.Lea",
            Description = "Job Processing Service",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

// Static endpoint handlers
public static class JobEndpoints
{
    public static void Register(WebApplication app)
    {
        app.MapGet("/health", async (IJobService service) => await service.GetHealthAsync());

        app.MapGet("/", () => "IO.Lea - Job Processing Service is running!");

        app.MapGet("/info", () => new
        {
            Service = "IO.Lea",
            Description = "Job Processing Service",
            Port = 8085,
            Endpoints = new[] { "/health", "/", "/info", "/swagger", "/api/job", "/api/job/{jobId}" },
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });

        app.MapPost("/api/job", async (JobRequest request, IJobService service) => 
            await service.CreateJobAsync(request));
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

public record JobResponse
{
    public string Service { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string ApplicationUrl { get; init; } = string.Empty;
    public bool GoogleJobsPosted { get; init; }
    public DateTime Timestamp { get; init; }
}

public record JobRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string ApplicationUrl { get; init; } = string.Empty;
}
