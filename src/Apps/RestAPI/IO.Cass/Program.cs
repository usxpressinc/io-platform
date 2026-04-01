using IO.Platform.Common.Core.Configuration;
using IO.Platform.Common.Core.Exceptions;
using IO.Platform.Common.Core.Lifecycle;
using IO.Platform.Common.Core.Monitoring;
using IO.Platform.Common.Core.Health;
using IO.Platform.Common.Infrastructure.MongoDb;
using IO.Cass.Infrastructure.Highway;
using IO.Cass.Infrastructure.McLeod;
using IO.Cass.Core.Carriers;
using IO.Cass.Infrastructure.Data;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var environment = Enum.Parse<MonitoringEnvironment>(
    configuration.GetValue<string>(EnvironmentVariables.ApplicationEnvironment) ?? "development",
    ignoreCase: true);
var project = configuration.GetValue<string>(EnvironmentVariables.ApplicationProject) ?? "io-platform";
var group = configuration.GetValue<string>(EnvironmentVariables.ApplicationGroup) ?? "carrier-vetting";

// Configure structured logging and OpenTelemetry
builder.Services.AddStructuredLogging(configuration, "IO.Cass");
builder.Services.AddOpenTelemetryInstrumentation(configuration, "IO.Cass");

// Add environment configuration management
builder.Services.AddEnvironmentConfiguration(configuration, "IO.Cass");

// Add monitoring (Grafana/OTEL)
builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
    EnableOtel = true,
});

// Add MongoDB using USXpress Configuration.Mongo
builder.AddMongoDb()
       .AddCarrierDataRepository();

// Add API clients
builder.Services.AddSingleton<IHighwayApiClient, HighwayApiClient>();
builder.Services.AddSingleton<IMcLeodApiClient, McLeodApiClient>();

// Add carrier vetting service
builder.Services.AddSingleton<ICarrierVettingService, CarrierVettingService>();

// Add repositories
builder.Services.AddSingleton<CarrierRepository>();

// Add graceful shutdown
builder.Services.AddSingleton<IGracefulShutdownComponent, HttpServerGracefulShutdown>();
builder.Services.AddHostedService<GracefulShutdownService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IO Cass API", Version = "v1" });
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

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IO Cass API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Add exception handling middleware
app.UseExceptionHandling();

app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }));

app.Run();
