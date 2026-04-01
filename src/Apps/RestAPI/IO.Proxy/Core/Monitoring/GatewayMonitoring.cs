using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace IO.Proxy.Core.Monitoring;

/// <summary>
/// Monitoring and metrics collection for API Gateway
/// Tracks request metrics, performance, and service health
/// </summary>
public interface IGatewayMonitoringService
{
    void RecordRequest(string serviceName, string method, string path, int statusCode, TimeSpan duration);
    void RecordError(string serviceName, string method, string path, string errorType);
    GatewayMetrics GetMetrics();
    ServiceMetrics GetServiceMetrics(string serviceName);
    Task ResetMetricsAsync();
}

/// <summary>
/// Gateway monitoring service implementation
/// </summary>
public class GatewayMonitoringService : IGatewayMonitoringService
{
    private readonly ILogger<GatewayMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, ServiceMetrics> _serviceMetrics;
    private readonly object _metricsLock = new();
    
    // Metrics
    private readonly Counter<int> _totalRequests;
    private readonly Counter<int> _totalErrors;
    private readonly Histogram<double> _requestDuration;
    private readonly Gauge<int> _activeRequests;

    public GatewayMonitoringService(ILogger<GatewayMonitoringService> logger, IMeterFactory meterFactory)
    {
        _logger = logger;
        _serviceMetrics = new ConcurrentDictionary<string, ServiceMetrics>();

        // Create metrics
        var meter = meterFactory.Create("IO.Proxy.Gateway");
        _totalRequests = meter.CreateCounter<int>("gateway_requests_total", "Total number of requests");
        _totalErrors = meter.CreateCounter<int>("gateway_errors_total", "Total number of errors");
        _requestDuration = meter.CreateHistogram<double>("gateway_request_duration_seconds", "Request duration in seconds");
        _activeRequests = meter.CreateGauge<int>("gateway_active_requests", "Number of active requests");

        _logger.LogInformation("Gateway monitoring service initialized");
    }

    public void RecordRequest(string serviceName, string method, string path, int statusCode, TimeSpan duration)
    {
        try
        {
            // Update global metrics
            _totalRequests.Add(1, new KeyValuePair<string, object?>("service", serviceName), 
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("status", statusCode));

            _requestDuration.Record(duration.TotalSeconds, new KeyValuePair<string, object?>("service", serviceName));

            // Update service-specific metrics
            var metrics = _serviceMetrics.GetOrAdd(serviceName, _ => new ServiceMetrics
            {
                ServiceName = serviceName
            });

            lock (metrics)
            {
                metrics.TotalRequests++;
                metrics.ResponseTimeSum += duration.TotalMilliseconds;
                metrics.LastRequest = DateTime.UtcNow;

                // Update status code counts
                metrics.StatusCodeCounts.AddOrUpdate(statusCode.ToString(), 1, (_, count) => count + 1);

                // Update method counts
                metrics.MethodCounts.AddOrUpdate(method, 1, (_, count) => count + 1);

                // Update success/failure counts
                if (statusCode >= 200 && statusCode < 300)
                {
                    metrics.SuccessfulRequests++;
                }
                else if (statusCode >= 400 && statusCode < 500)
                {
                    metrics.ClientErrors++;
                }
                else if (statusCode >= 500)
                {
                    metrics.ServerErrors++;
                }

                // Update average response time
                metrics.AverageResponseTime = metrics.ResponseTimeSum / metrics.TotalRequests;
            }

            // Log slow requests
            if (duration > TimeSpan.FromSeconds(1))
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} to {Service} took {Duration}ms",
                    method, path, serviceName, duration.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request metrics");
        }
    }

    public void RecordError(string serviceName, string method, string path, string errorType)
    {
        try
        {
            // Update global error metrics
            _totalErrors.Add(1, new KeyValuePair<string, object?>("service", serviceName),
                new KeyValuePair<string, object?>("error_type", errorType));

            // Update service-specific metrics
            var metrics = _serviceMetrics.GetOrAdd(serviceName, _ => new ServiceMetrics
            {
                ServiceName = serviceName
            });

            lock (metrics)
            {
                metrics.TotalErrors++;
                metrics.LastError = DateTime.UtcNow;
                metrics.ErrorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
            }

            _logger.LogError(
                "Error recorded for {Service} {Method} {Path}: {ErrorType}",
                serviceName, method, path, errorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording error metrics");
        }
    }

    public GatewayMetrics GetMetrics()
    {
        var totalRequests = 0;
        var totalErrors = 0;
        var totalResponseTime = 0.0;
        var services = new List<ServiceMetrics>();

        foreach (var serviceMetrics in _serviceMetrics.Values)
        {
            lock (serviceMetrics)
            {
                totalRequests += serviceMetrics.TotalRequests;
                totalErrors += serviceMetrics.TotalErrors;
                totalResponseTime += serviceMetrics.ResponseTimeSum;
                services.Add(CloneServiceMetrics(serviceMetrics));
            }
        }

        return new GatewayMetrics
        {
            TotalRequests = totalRequests,
            TotalErrors = totalErrors,
            AverageResponseTime = totalRequests > 0 ? totalResponseTime / totalRequests : 0,
            ErrorRate = totalRequests > 0 ? (double)totalErrors / totalRequests : 0,
            Services = services,
            LastUpdated = DateTime.UtcNow
        };
    }

    public ServiceMetrics GetServiceMetrics(string serviceName)
    {
        if (_serviceMetrics.TryGetValue(serviceName, out var metrics))
        {
            lock (metrics)
            {
                return CloneServiceMetrics(metrics);
            }
        }

        return new ServiceMetrics { ServiceName = serviceName };
    }

    public async Task ResetMetricsAsync()
    {
        await Task.Run(() =>
        {
            lock (_metricsLock)
            {
                foreach (var metrics in _serviceMetrics.Values)
                {
                    lock (metrics)
                    {
                        metrics.Reset();
                    }
                }

                _logger.LogInformation("Gateway metrics reset");
            }
        });
    }

    private ServiceMetrics CloneServiceMetrics(ServiceMetrics original)
    {
        return new ServiceMetrics
        {
            ServiceName = original.ServiceName,
            TotalRequests = original.TotalRequests,
            SuccessfulRequests = original.SuccessfulRequests,
            ClientErrors = original.ClientErrors,
            ServerErrors = original.ServerErrors,
            TotalErrors = original.TotalErrors,
            ResponseTimeSum = original.ResponseTimeSum,
            AverageResponseTime = original.AverageResponseTime,
            LastRequest = original.LastRequest,
            LastError = original.LastError,
            StatusCodeCounts = new Dictionary<string, int>(original.StatusCodeCounts),
            MethodCounts = new Dictionary<string, int>(original.MethodCounts),
            ErrorCounts = new Dictionary<string, int>(original.ErrorCounts)
        };
    }

    public void IncrementActiveRequests()
    {
        _activeRequests.Record(1);
    }

    public void DecrementActiveRequests()
    {
        _activeRequests.Record(-1);
    }
}

/// <summary>
/// Gateway metrics
/// </summary>
public class GatewayMetrics
{
    public int TotalRequests { get; set; }
    public int TotalErrors { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public List<ServiceMetrics> Services { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Service metrics
/// </summary>
public class ServiceMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int ClientErrors { get; set; }
    public int ServerErrors { get; set; }
    public int TotalErrors { get; set; }
    public double ResponseTimeSum { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime LastRequest { get; set; }
    public DateTime? LastError { get; set; }
    public Dictionary<string, int> StatusCodeCounts { get; set; } = new();
    public Dictionary<string, int> MethodCounts { get; set; } = new();
    public Dictionary<string, int> ErrorCounts { get; set; } = new();

    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    public double ClientErrorRate => TotalRequests > 0 ? (double)ClientErrors / TotalRequests : 0;
    public double ServerErrorRate => TotalRequests > 0 ? (double)ServerErrors / TotalRequests : 0;

    public void Reset()
    {
        TotalRequests = 0;
        SuccessfulRequests = 0;
        ClientErrors = 0;
        ServerErrors = 0;
        TotalErrors = 0;
        ResponseTimeSum = 0;
        AverageResponseTime = 0;
        LastRequest = DateTime.MinValue;
        LastError = null;
        StatusCodeCounts.Clear();
        MethodCounts.Clear();
        ErrorCounts.Clear();
    }
}

/// <summary>
/// Request tracking middleware for monitoring
/// </summary>
public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IGatewayMonitoringService _monitoringService;
    private readonly ILogger<RequestTrackingMiddleware> _logger;

    public RequestTrackingMiddleware(
        RequestDelegate next,
        IGatewayMonitoringService monitoringService,
        ILogger<RequestTrackingMiddleware> logger)
    {
        _next = next;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var serviceName = context.Items["ServiceName"]?.ToString() ?? "unknown";
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        // Increment active requests
        _monitoringService.IncrementActiveRequests();

        try
        {
            await _next(context);

            // Record successful request
            var duration = DateTime.UtcNow - startTime;
            _monitoringService.RecordRequest(serviceName, method, path, context.Response.StatusCode, duration);
        }
        catch (Exception ex)
        {
            // Record error
            var duration = DateTime.UtcNow - startTime;
            _monitoringService.RecordError(serviceName, method, path, ex.GetType().Name);
            _monitoringService.RecordRequest(serviceName, method, path, 500, duration);
            throw;
        }
        finally
        {
            // Decrement active requests
            _monitoringService.DecrementActiveRequests();
        }
    }
}

/// <summary>
/// Extension methods for monitoring
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds gateway monitoring services
    /// </summary>
    public static IServiceCollection AddGatewayMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<IGatewayMonitoringService, GatewayMonitoringService>();
        return services;
    }

    /// <summary>
    /// Adds request tracking middleware
    /// </summary>
    public static IApplicationBuilder UseRequestTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTrackingMiddleware>();
    }
}
