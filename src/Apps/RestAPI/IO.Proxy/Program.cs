using IO.Core.Authentication;
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

// Configure Serilog with console output
var loggingConfiguration = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

// Add monitoring (Grafana/OTEL)
builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
    EnableOtel = true,
    SerilogLoggerConfiguration = loggingConfiguration,
});

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

// Add HTTP clients for downstream services
builder.Services.AddHttpClient(ServiceEndpoints.CommonService, client =>
{
    client.BaseAddress = new Uri(configuration["Services:Common:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
});

builder.Services.AddHttpClient(ServiceEndpoints.CassService, client =>
{
    client.BaseAddress = new Uri(configuration["Services:Cass:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
});

builder.Services.AddHttpClient(ServiceEndpoints.ElsaService, client =>
{
    client.BaseAddress = new Uri(configuration["Services:Elsa:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
});

builder.Services.AddHttpClient(ServiceEndpoints.LarryService, client =>
{
    client.BaseAddress = new Uri(configuration["Services:Larry:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
});

builder.Services.AddHttpClient(ServiceEndpoints.LeaService, client =>
{
    client.BaseAddress = new Uri(configuration["Services:Lea:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
});

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

// Add CORS
app.UseCors("AllowAll");

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
