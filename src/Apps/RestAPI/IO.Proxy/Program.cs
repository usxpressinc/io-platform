using IO.Core.Authentication;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using IO.Proxy.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure USXpress monitoring
var environment = Enum.Parse<MonitoringEnvironment>(
    builder.Configuration["APPLICATION:ENVIRONMENT"] ?? "development",
    ignoreCase: true);
var project = builder.Configuration["APPLICATION:PROJECT"] ?? "io-platform";
var group = builder.Configuration["APPLICATION:GROUP"] ?? "gateway";

builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = builder.Configuration["REVISION"] ?? "1.0.0",
    EnableOtel = true,
    SerilogLoggerConfiguration = new LoggerConfiguration().WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"),
});

// Add HTTP clients for downstream services
builder.Services.AddHttpClient("io-common", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Common:BaseUrl"] ?? "https://api.demo.poc.dev.usxpress.io");
});

builder.Services.AddHttpClient("io-cass", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Cass:BaseUrl"] ?? "https://api.demo.poc.qa.usxpress.io");
});

builder.Services.AddHttpClient("io-elsa", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Elsa:BaseUrl"] ?? "https://api.demo.poc.qa.usxpress.io");
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
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add X-Auth token authentication
builder.Services.AddXAuthTokenAuthentication(builder.Configuration);

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

// Add X-Auth token authentication
app.UseXAuthTokenAuthentication();

// Add authorization (for additional checks if needed)
app.UseAuthorization();

app.MapControllers();

// Map proxy routes
app.MapGatewayRoutes();

// Add monitoring endpoints (includes health/ready automatically)
app.MonitoringEndpoints();

// API versioning
app.MapGet("/", () => Results.Json(new 
{ 
    service = "IO Proxy API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    documentation = "/swagger",
    endpoints = new
    {
        email = "/api/common/email",
        carrierValidation = "/api/clara/carriers/valid", 
        pricing = "/api/elsa/price/lookup",
        health = "/health",  // Provided by USXpress.Monitoring
        metrics = "/metrics" // Provided by USXpress.Monitoring
    }
}));

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
