using IO.Core.Authentication;
using IO.Proxy.Routes;
using IO.Proxy.Routing;
using Microsoft.OpenApi.Models;
using Serilog;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure USXpress monitoring
var environment = Enum.Parse<MonitoringEnvironment>(
    builder.Configuration["APPLICATION:ENVIRONMENT"] ?? "development",
    ignoreCase: true
);
var project = builder.Configuration["APPLICATION:PROJECT"] ?? "io-platform";
var group = builder.Configuration["APPLICATION:GROUP"] ?? "gateway";

builder.AddMonitoring(
    new MonitoringOptions
    {
        ProjectGroup = group,
        ProjectName = project,
        Environment = environment,
        ReleaseVersion = builder.Configuration["REVISION"] ?? "1.0.0",
        EnableOtel = true,
        SerilogLoggerConfiguration = new LoggerConfiguration().WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        ),
    }
);

// Configure service route options
builder.Services.Configure<ServiceRouteOptions>(builder.Configuration.GetSection("ServiceRoutes"));

// Register service route registry
builder.Services.AddSingleton<ServiceRouteRegistry>();
builder.Services.AddSingleton<OAuthAuthHandlerFactory>();
builder.Services.AddSingleton<NetworkCredentialsAuthHandlerFactory>();

// Add HTTP clients for downstream services with auth handlers
builder.Services.AddHttpClient(
    "io-common",
    (serviceProvider, client) =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:Common:BaseUrl"]
                ?? "https://api.demo.poc.dev.usxpress.io"
        );
    }
).AddHttpMessageHandler(
    serviceProvider => serviceProvider.GetRequiredService<OAuthAuthHandlerFactory>().CreateHandler("/api/common")
);

builder.Services.AddHttpClient(
    "io-cass",
    (serviceProvider, client) =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:Cass:BaseUrl"] ?? "https://api.demo.poc.qa.usxpress.io"
        );
    }
).AddHttpMessageHandler(
    serviceProvider => serviceProvider.GetRequiredService<OAuthAuthHandlerFactory>().CreateHandler("/api/clara")
);

builder.Services.AddHttpClient(
    "io-elsa",
    (serviceProvider, client) =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:Elsa:BaseUrl"] ?? "https://api.demo.poc.qa.usxpress.io"
        );
    }
).AddHttpMessageHandler(
    serviceProvider => serviceProvider.GetRequiredService<OAuthAuthHandlerFactory>().CreateHandler("/api/elsa")
);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IO Proxy API", Version = "v1" });
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "X-Auth token (Bearer token or X-Auth-Token header)",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

// Add X-Auth token authentication
builder.Services.AddXAuthTokenAuthentication(builder.Configuration);

// Add cross-origin resource sharing
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
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
app.MapGet(
    "/",
    () =>
        Results.Json(
            new
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
                    health = "/health", // Provided by USXpress.Monitoring
                    metrics = "/metrics", // Provided by USXpress.Monitoring
                },
            }
        )
);

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
