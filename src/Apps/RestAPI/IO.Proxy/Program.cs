using IO.Core.Authentication;
using IO.Platform.Common.Core.Configuration;
using IO.Platform.Common.Core.Exceptions;
using IO.Platform.Common.Core.Lifecycle;
using IO.Platform.Common.Core.Monitoring;
using IO.Platform.Common.Core.Health;
using IO.Proxy.Core.Routing;
using IO.Proxy.Core.LoadBalancing;
using IO.Proxy.Core.Authentication;
using IO.Proxy.Infrastructure.Http;
using IO.Proxy.Middleware;
using IO.Proxy.Core.Health;
using IO.Proxy.Core.Monitoring;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using IO.Core.Constants;

var builder = WebApplication.CreateBuilder(args);

// Application metadata from configuration
var configuration = builder.Configuration;
var environment = Enum.Parse<MonitoringEnvironment>(
    configuration.GetValue<string>(EnvironmentVariables.ApplicationEnvironment) ?? "development",
    ignoreCase: true);
var project = configuration.GetValue<string>(EnvironmentVariables.ApplicationProject) ?? "io-platform";
var group = configuration.GetValue<string>(EnvironmentVariables.ApplicationGroup) ?? "gateway";

// Configure structured logging and OpenTelemetry
builder.Services.AddStructuredLogging(configuration, "IO.Proxy");
builder.Services.AddOpenTelemetryInstrumentation(configuration, "IO.Proxy");

// Add environment configuration management
builder.Services.AddEnvironmentConfiguration(configuration, "IO.Proxy");

// Add monitoring (Grafana/OTEL)
builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
    EnableOtel = true,
});

// Add gateway monitoring
builder.Services.AddGatewayMonitoring();

// Add health aggregation
builder.Services.AddSingleton<IHealthAggregationService, HealthAggregationService>();
builder.Services.Configure<HealthAggregationSettings>(settings =>
{
    settings.StartedAt = DateTime.UtcNow;
});

// Add authentication forwarding
builder.Services.AddAuthenticationForwarding(configuration);

// Add gateway routing and load balancing
builder.Services.AddGatewayRouting(configuration);

// Add HTTP clients for downstream services
builder.Services.AddGatewayHttpClients(configuration);

// Add graceful shutdown
builder.Services.AddSingleton<IGracefulShutdownComponent, HttpServerGracefulShutdown>();
builder.Services.AddHostedService<GracefulShutdownService>();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IO Proxy API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "X-Auth token (Bearer token or X-Auth-Token header)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Dictionary<string, string[]>
    {
        { "Bearer", Array.Empty<string>() }
    });
});

// Add X-Auth token authentication
builder.Services.AddXAuthTokenAuthentication(configuration);

// Add cross-origin resource sharing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IO Proxy API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Add exception handling middleware
app.UseExceptionHandling();

// Add request tracking middleware
app.UseRequestTracking();

// Add CORS
app.UseCors("AllowAll");

// Add request/response transformation middleware
app.UseRequestTransformation();

// Add authentication forwarding
app.UseAuthenticationForwarding();

// Add X-Auth token authentication
app.UseXAuthTokenAuthentication();

// Add authorization (for additional checks if needed)
app.UseAuthorization();

app.MapControllers();

// Health endpoints (no authentication required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.MapGet("/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }))
   .WithName("ReadinessCheck");

// API versioning
app.MapGet("/", () => Results.Json(new 
{ 
    service = "IO Proxy API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    documentation = "/swagger"
}));

app.Run();
