using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace IO.Proxy.Endpoints;

/// <summary>
/// API Gateway endpoints for health, metrics, and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly IHealthAggregationService _healthService;
    private readonly IGatewayMonitoringService _monitoringService;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        IHealthAggregationService healthService,
        IGatewayMonitoringService monitoringService,
        ILogger<GatewayController> logger)
    {
        _healthService = healthService;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// Gateway health check endpoint
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var health = await _healthService.GetOverallHealthAsync();
            
            return health.Status switch
            {
                HealthStatus.Healthy => Ok(new { status = "healthy", health }),
                HealthStatus.Degraded => Ok(new { status = "degraded", health }),
                HealthStatus.Unhealthy => StatusCode(503, new { status = "unhealthy", health }),
                _ => StatusCode(500, new { status = "unknown", health })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway health");
            return StatusCode(500, new { status = "error", message = "Health check failed" });
        }
    }

    /// <summary>
    /// Gateway readiness check endpoint
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var health = await _healthService.GetOverallHealthAsync();
            
            // Gateway is ready if it's not unhealthy
            if (health.Status == HealthStatus.Unhealthy)
            {
                return StatusCode(503, new { 
                    status = "not_ready", 
                    reason = "Gateway is unhealthy",
                    unhealthyServices = health.UnhealthyServices
                });
            }

            return Ok(new { 
                status = "ready",
                version = "1.0.0",
                uptime = health.Uptime,
                services = health.Services.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking gateway readiness");
            return StatusCode(500, new { status = "error", message = "Readiness check failed" });
        }
    }

    /// <summary>
    /// Gateway metrics endpoint
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        try
        {
            var metrics = _monitoringService.GetMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway metrics");
            return StatusCode(500, new { status = "error", message = "Metrics retrieval failed" });
        }
    }

    /// <summary>
    /// Service-specific health check endpoint
    /// </summary>
    [HttpGet("services/{serviceName}/health")]
    public async Task<IActionResult> GetServiceHealth(string serviceName)
    {
        try
        {
            var health = await _healthService.GetServiceHealthAsync(serviceName);
            
            return health.Status switch
            {
                HealthStatus.Healthy => Ok(new { status = "healthy", service = health }),
                HealthStatus.Degraded => Ok(new { status = "degraded", service = health }),
                HealthStatus.Unhealthy => StatusCode(503, new { status = "unhealthy", service = health }),
                HealthStatus.Unknown => StatusCode(503, new { status = "unknown", service = health }),
                _ => StatusCode(500, new { status = "error", service = health })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service health for {ServiceName}", serviceName);
            return StatusCode(500, new { status = "error", message = $"Health check failed for {serviceName}" });
        }
    }

    /// <summary>
    /// Service-specific metrics endpoint
    /// </summary>
    [HttpGet("services/{serviceName}/metrics")]
    public IActionResult GetServiceMetrics(string serviceName)
    {
        try
        {
            var metrics = _monitoringService.GetServiceMetrics(serviceName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service metrics for {ServiceName}", serviceName);
            return StatusCode(500, new { status = "error", message = $"Metrics retrieval failed for {serviceName}" });
        }
    }

    /// <summary>
    /// Refresh service health endpoint
    /// </summary>
    [HttpPost("services/{serviceName}/health/refresh")]
    public async Task<IActionResult> RefreshServiceHealth(string serviceName)
    {
        try
        {
            await _healthService.RefreshServiceHealthAsync(serviceName);
            var health = await _healthService.GetServiceHealthAsync(serviceName);
            
            return Ok(new { 
                status = "refreshed",
                service = health,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing service health for {ServiceName}", serviceName);
            return StatusCode(500, new { status = "error", message = $"Health refresh failed for {serviceName}" });
        }
    }

    /// <summary>
    /// Reset metrics endpoint (for testing/maintenance)
    /// </summary>
    [HttpPost("metrics/reset")]
    public async Task<IActionResult> ResetMetrics()
    {
        try
        {
            await _monitoringService.ResetMetricsAsync();
            return Ok(new { 
                status = "reset",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics");
            return StatusCode(500, new { status = "error", message = "Metrics reset failed" });
        }
    }

    /// <summary>
    /// Gateway information endpoint
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        try
        {
            var info = new
            {
                name = "IO.Proxy Gateway",
                version = "1.0.0",
                description = "API Gateway for IO Platform microservices",
                startedAt = DateTime.UtcNow.AddHours(-1), // This would come from actual startup time
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                frameworks = new[]
                {
                    "ASP.NET Core 10.0",
                    "USXpress.Monitoring",
                    "USXpress.Configuration.Mongo",
                    "USXpress.Kafka"
                },
                capabilities = new[]
                {
                    "Request routing",
                    "Load balancing",
                    "Authentication forwarding",
                    "Request/response transformation",
                    "Health check aggregation",
                    "Metrics collection"
                }
            };

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway info");
            return StatusCode(500, new { status = "error", message = "Info retrieval failed" });
        }
    }

    /// <summary>
    /// Service registry endpoint
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        try
        {
            var health = await _healthService.GetOverallHealthAsync();
            
            var services = health.Services.Select(s => new
            {
                name = s.ServiceName,
                status = s.Status.ToString().ToLowerInvariant(),
                lastCheck = s.LastCheck,
                responseTime = s.ResponseTime,
                error = s.ErrorMessage,
                dependencies = s.Dependencies?.Select(d => new
                {
                    name = d.Name,
                    status = d.Status.ToString().ToLowerInvariant(),
                    responseTime = d.ResponseTime
                })
            });

            return Ok(new { 
                services,
                total = services.Count(),
                healthy = services.Count(s => s.status == "healthy"),
                degraded = services.Count(s => s.status == "degraded"),
                unhealthy = services.Count(s => s.status == "unhealthy")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service registry");
            return StatusCode(500, new { status = "error", message = "Service registry retrieval failed" });
        }
    }
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
