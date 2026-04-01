using IO.Platform.Common.Core.Configuration;
using IO.Platform.Common.Core.Exceptions;
using IO.Platform.Common.Core.Lifecycle;
using IO.Platform.Common.Core.Monitoring;
using IO.Platform.Common.Core.Health;
using IO.Platform.Common.Infrastructure.MongoDb;
using IO.Common.Infrastructure.Email;
using IO.Common.Core.Email;
using IO.Common.Core;
using IO.Common.Core.Users;
using IO.Common.Infrastructure.Data;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var environment = Enum.Parse<MonitoringEnvironment>(
    configuration.GetValue<string>(EnvironmentVariables.ApplicationEnvironment) ?? "development",
    ignoreCase: true);
var project = configuration.GetValue<string>(EnvironmentVariables.ApplicationProject) ?? "io-platform";
var group = configuration.GetValue<string>(EnvironmentVariables.ApplicationGroup) ?? "common";

// Configure structured logging and OpenTelemetry
builder.Services.AddStructuredLogging(configuration, "IO.Common");
builder.Services.AddOpenTelemetryInstrumentation(configuration, "IO.Common");

// Add environment configuration management
builder.Services.AddEnvironmentConfiguration(configuration, "IO.Common");

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
       .AddEmailLogsRepository()
       .AddContextDataRepository();

// Add email service
builder.Services.AddSingleton<IEmailService, SendGridService>();
builder.Services.AddSingleton<IEmailBusinessService, EmailBusinessService>();

// Add context service
builder.Services.AddSingleton<IContextService, ContextService>();

// Add user context service
builder.Services.AddSingleton<IUserContextBusinessService, UserContextBusinessService>();

// Add repositories
builder.Services.AddSingleton<EmailLogRepository>();
builder.Services.AddSingleton<UserContextRepository>();

// Add graceful shutdown
builder.Services.AddSingleton<IGracefulShutdownComponent, HttpServerGracefulShutdown>();
builder.Services.AddHostedService<GracefulShutdownService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IO Common API", Version = "v1" });
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IO Common API v1");
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
