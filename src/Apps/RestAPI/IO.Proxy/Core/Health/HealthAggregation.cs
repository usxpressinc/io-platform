using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Proxy.Core.Health;

/// <summary>
/// Health check aggregation service for API Gateway
/// Monitors health of all downstream services and provides aggregated status
/// </summary>
public interface IHealthAggregationService
{
    Task<GatewayHealthResult> GetOverallHealthAsync();
    Task<ServiceHealthStatus> GetServiceHealthAsync(string serviceName);
    Task RefreshServiceHealthAsync(string serviceName);
    void RegisterService(string serviceName, string healthEndpoint);
    void UnregisterService(string serviceName);
}

/// <summary>
/// Health aggregation service implementation
/// </summary>
public class HealthAggregationService : IHealthAggregationService, IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthAggregationService> _logger;
    private readonly HealthAggregationSettings _settings;
    private readonly Timer _healthCheckTimer;
    private readonly ConcurrentDictionary<string, ServiceHealthInfo> _services;

    public HealthAggregationService(
        IHttpClientFactory httpClientFactory,
        ILogger<HealthAggregationService> logger,
        HealthAggregationSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings;
        _services = new ConcurrentDictionary<string, ServiceHealthInfo>();

        // Register default services
        RegisterDefaultServices();

        // Start periodic health checks
        _healthCheckTimer = new Timer(
            async _ => await PerformHealthChecksAsync(),
            null,
            TimeSpan.Zero,
            _settings.HealthCheckInterval);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health aggregation service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _healthCheckTimer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("Health aggregation service stopped");
        return Task.CompletedTask;
    }

    public async Task<GatewayHealthResult> GetOverallHealthAsync()
    {
        var services = new List<ServiceHealthStatus>();
        var overallStatus = HealthStatus.Healthy;
        var totalIssues = 0;

        foreach (var service in _services.Values)
        {
            var serviceStatus = new ServiceHealthStatus
            {
                ServiceName = service.ServiceName,
                Status = service.Status,
                LastCheck = service.LastCheck,
                ResponseTime = service.ResponseTime,
                ErrorMessage = service.ErrorMessage,
                Dependencies = service.Dependencies?.Select(dep => new ServiceDependencyStatus
                {
                    Name = dep.Name,
                    Status = dep.Status,
                    ResponseTime = dep.ResponseTime
                }).ToList()
            };

            services.Add(serviceStatus);

            if (service.Status == HealthStatus.Unhealthy)
            {
                overallStatus = HealthStatus.Unhealthy;
                totalIssues++;
            }
            else if (service.Status == HealthStatus.Degraded && overallStatus == HealthStatus.Healthy)
            {
                overallStatus = HealthStatus.Degraded;
                totalIssues++;
            }
        }

        return new GatewayHealthResult
        {
            Status = overallStatus,
            Services = services,
            TotalServices = services.Count,
            HealthyServices = services.Count(s => s.Status == HealthStatus.Healthy),
            DegradedServices = services.Count(s => s.Status == HealthStatus.Degraded),
            UnhealthyServices = services.Count(s => s.Status == HealthStatus.Unhealthy),
            LastCheck = DateTime.UtcNow,
            TotalIssues = totalIssues,
            GatewayVersion = "1.0.0",
            Uptime = DateTime.UtcNow.Subtract(_settings.StartedAt)
        };
    }

    public async Task<ServiceHealthStatus> GetServiceHealthAsync(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var serviceInfo))
        {
            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unknown,
                ErrorMessage = "Service not registered"
            };
        }

        // Force a fresh health check
        await RefreshServiceHealthAsync(serviceName);

        return new ServiceHealthStatus
        {
            ServiceName = serviceInfo.ServiceName,
            Status = serviceInfo.Status,
            LastCheck = serviceInfo.LastCheck,
            ResponseTime = serviceInfo.ResponseTime,
            ErrorMessage = serviceInfo.ErrorMessage,
            Dependencies = serviceInfo.Dependencies?.Select(dep => new ServiceDependencyStatus
            {
                Name = dep.Name,
                Status = dep.Status,
                ResponseTime = dep.ResponseTime
            }).ToList()
        };
    }

    public async Task RefreshServiceHealthAsync(string serviceName)
    {
        if (_services.TryGetValue(serviceName, out var serviceInfo))
        {
            await CheckServiceHealthAsync(serviceInfo);
        }
    }

    public void RegisterService(string serviceName, string healthEndpoint)
    {
        _services.AddOrUpdate(serviceName,
            new ServiceHealthInfo
            {
                ServiceName = serviceName,
                HealthEndpoint = healthEndpoint,
                Status = HealthStatus.Unknown,
                RegisteredAt = DateTime.UtcNow
            },
            (key, existing) =>
            {
                existing.HealthEndpoint = healthEndpoint;
                existing.Status = HealthStatus.Unknown;
                return existing;
            });

        _logger.LogInformation("Registered service {ServiceName} with health endpoint {Endpoint}", 
            serviceName, healthEndpoint);
    }

    public void UnregisterService(string serviceName)
    {
        if (_services.TryRemove(serviceName, out _))
        {
            _logger.LogInformation("Unregistered service {ServiceName}", serviceName);
        }
    }

    private void RegisterDefaultServices()
    {
        // Register default IO Platform services
        RegisterService("IO.Common", "http://io-common:8081/health");
        RegisterService("IO.Cass", "http://io-cass:8082/health");
        RegisterService("IO.Elsa", "http://io-elsa:8083/health");
        RegisterService("IO.Larry", "http://io-larry:8084/health");
        RegisterService("IO.Lea", "http://io-lea:8085/health");
    }

    private async Task PerformHealthChecksAsync()
    {
        var tasks = _services.Values.Select(CheckServiceHealthAsync);
        await Task.WhenAll(tasks);
    }

    private async Task CheckServiceHealthAsync(ServiceHealthInfo serviceInfo)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(serviceInfo.HealthEndpoint);
            var responseTime = DateTime.UtcNow - startTime;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var healthData = ParseHealthResponse(content);

                serviceInfo.Status = DetermineServiceStatus(healthData);
                serviceInfo.ResponseTime = responseTime.TotalMilliseconds;
                serviceInfo.ErrorMessage = null;
                serviceInfo.Dependencies = healthData.Dependencies;
            }
            else
            {
                serviceInfo.Status = HealthStatus.Unhealthy;
                serviceInfo.ResponseTime = responseTime.TotalMilliseconds;
                serviceInfo.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                serviceInfo.Dependencies = null;
            }
        }
        catch (HttpRequestException ex)
        {
            serviceInfo.Status = HealthStatus.Unhealthy;
            serviceInfo.ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            serviceInfo.ErrorMessage = ex.Message;
            serviceInfo.Dependencies = null;
        }
        catch (TaskCanceledException)
        {
            serviceInfo.Status = HealthStatus.Degraded;
            serviceInfo.ResponseTime = _settings.HealthCheckTimeout.TotalMilliseconds;
            serviceInfo.ErrorMessage = "Health check timed out";
            serviceInfo.Dependencies = null;
        }
        catch (Exception ex)
        {
            serviceInfo.Status = HealthStatus.Unhealthy;
            serviceInfo.ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            serviceInfo.ErrorMessage = ex.Message;
            serviceInfo.Dependencies = null;
        }

        serviceInfo.LastCheck = DateTime.UtcNow;
    }

    private HealthData ParseHealthResponse(string content)
    {
        try
        {
            var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content);
            if (healthResponse != null)
            {
                return new HealthData
                {
                    Status = healthResponse.Status,
                    Dependencies = healthResponse.Dependencies?.Select(dep => new DependencyHealth
                    {
                        Name = dep.Name,
                        Status = dep.Status,
                        ResponseTime = dep.ResponseTime
                    }).ToList()
                };
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, assume healthy if response was successful
        }

        return new HealthData
        {
            Status = HealthStatus.Healthy,
            Dependencies = null
        };
    }

    private HealthStatus DetermineServiceStatus(HealthData healthData)
    {
        if (healthData.Dependencies?.Any() == true)
        {
            var unhealthyDeps = healthData.Dependencies.Count(d => d.Status == HealthStatus.Unhealthy);
            var degradedDeps = healthData.Dependencies.Count(d => d.Status == HealthStatus.Degraded);

            if (unhealthyDeps > 0)
            {
                return HealthStatus.Unhealthy;
            }
            else if (degradedDeps > 0)
            {
                return HealthStatus.Degraded;
            }
        }

        return healthData.Status;
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}

/// <summary>
/// Health aggregation settings
/// </summary>
public class HealthAggregationSettings
{
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public bool IncludeDetailedDependencies { get; set; } = true;
}

/// <summary>
/// Service health information
/// </summary>
public class ServiceHealthInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string HealthEndpoint { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public DateTime LastCheck { get; set; }
    public double ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public List<DependencyHealth>? Dependencies { get; set; }
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// Gateway health result
/// </summary>
public class GatewayHealthResult
{
    public HealthStatus Status { get; set; }
    public List<ServiceHealthStatus> Services { get; set; } = new();
    public int TotalServices { get; set; }
    public int HealthyServices { get; set; }
    public int DegradedServices { get; set; }
    public int UnhealthyServices { get; set; }
    public DateTime LastCheck { get; set; }
    public int TotalIssues { get; set; }
    public string GatewayVersion { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Service health status
/// </summary>
public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public DateTime LastCheck { get; set; }
    public double ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ServiceDependencyStatus>? Dependencies { get; set; }
}

/// <summary>
/// Service dependency status
/// </summary>
public class ServiceDependencyStatus
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public double ResponseTime { get; set; }
}

/// <summary>
/// Health data from service response
/// </summary>
public class HealthData
{
    public HealthStatus Status { get; set; }
    public List<DependencyHealth>? Dependencies { get; set; }
}

/// <summary>
/// Health response from downstream service
/// </summary>
public class HealthResponse
{
    public HealthStatus Status { get; set; }
    public List<HealthDependency>? Dependencies { get; set; }
}

/// <summary>
/// Health dependency from service response
/// </summary>
public class HealthDependency
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public double ResponseTime { get; set; }
}

/// <summary>
/// Dependency health information
/// </summary>
public class DependencyHealth
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public double ResponseTime { get; set; }
}
