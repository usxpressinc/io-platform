using IO.Clara.Infrastructure.Highway;

namespace IO.Clara.Core;

/// <summary>
/// Core dependency injection configuration for IO.Clara
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add core services to the container
    /// </summary>
    public static IHostApplicationBuilder AddCore(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        // Add structured logging
        services.AddLogging(b =>
        {
            b.AddConsole();
            b.AddDebug();
        });

        // Add OpenTelemetry
        services
            .AddOpenTelemetry()
            .WithTracing(b => b.AddSource("IO.Clara"))
            .WithMetrics(b => b.AddMeter("IO.Clara"));

        // Add environment configuration management
        services.Configure<HighwayApiSettings>(configuration.GetSection("HighwayApi"));

        // Add monitoring (Grafana/OTEL)
        services.AddHealthChecks();

        // Adds a graceful shutdown
        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        return builder;
    }
}
