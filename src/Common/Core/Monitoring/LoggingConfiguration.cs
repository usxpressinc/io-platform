using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;

namespace IO.Platform.Common.Core.Monitoring;

/// <summary>
/// Centralized logging configuration for all IO Platform services
/// Follows USXpress monitoring standards with structured logging and correlation IDs
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure structured logging with Serilog and OpenTelemetry integration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">Name of the service for log context</param>
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string serviceName)
    {
        // Configure Serilog
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development")
            .Enrich.WithCorrelationIdHeader()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");

        // Add OpenTelemetry endpoint if configured
        var otelEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrEmpty(otelEndpoint))
        {
            loggerConfiguration.WriteTo.OpenTelemetry();
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        // Add logging to services
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        return services;
    }

    /// <summary>
    /// Configure OpenTelemetry for distributed tracing and metrics
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">Name of the service for telemetry</param>
    public static IServiceCollection AddOpenTelemetryInstrumentation(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otelEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrEmpty(otelEndpoint))
        {
            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter())
                .WithMetrics(builder => builder
                    .AddMeter(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter());
        }

        return services;
    }
}

/// <summary>
/// Serilog enrichment for correlation ID from HTTP headers
/// </summary>
public static class CorrelationIdEnrichmentExtensions
{
    public static LoggerConfiguration WithCorrelationIdHeader(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.With(new CorrelationIdEnricher());
    }
}

/// <summary>
/// Enricher to extract correlation ID from HTTP context
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("RequestId", out var requestId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", requestId));
        }
    }
}
