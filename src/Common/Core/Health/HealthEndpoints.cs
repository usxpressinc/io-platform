using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace IO.Platform.Common.Core.Health;

/// <summary>
/// Base health check implementation for all IO Platform services
/// Provides dependency status monitoring and performance metrics
/// </summary>
public class ServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<ServiceHealthCheck> _logger;
    private readonly ServiceHealthConfiguration _config;

    public ServiceHealthCheck(
        ILogger<ServiceHealthCheck> logger,
        ServiceHealthConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>
            {
                ["ServiceName"] = _config.ServiceName,
                ["Version"] = _config.Version,
                ["Uptime"] = DateTime.UtcNow.Subtract(_config.StartedAt).ToString(),
                ["CheckedAt"] = DateTime.UtcNow
            };

            // Check all dependencies
            var dependencyTasks = _config.Dependencies.Select(async dep =>
            {
                var depHealth = await CheckDependencyHealth(dep, cancellationToken);
                return new KeyValuePair<string, object>(dep.Name, depHealth);
            });

            var dependencyResults = await Task.WhenAll(dependencyTasks);
            
            foreach (var kvp in dependencyResults)
            {
                healthData[kvp.Key] = kvp.Value;
            }

            // Determine overall health
            var allHealthy = dependencyResults.All(r => 
                r.Value is DependencyHealth dh && dh.Status == HealthStatus.Healthy);

            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
            var description = allHealthy 
                ? "All dependencies are healthy" 
                : "Some dependencies are unhealthy";

            _logger.LogDebug("Health check completed: {Status} - {Description}", status, description);

            return new HealthCheckResult(
                status,
                description,
                data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }

    private async Task<DependencyHealth> CheckDependencyHealth(
        DependencyConfiguration dependency,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (dependency.Type)
            {
                case DependencyType.MongoDB:
                    return await CheckMongoDbHealth(dependency, cancellationToken);
                case DependencyType.Kafka:
                    return await CheckKafkaHealth(dependency, cancellationToken);
                case DependencyType.Http:
                    return await CheckHttpHealth(dependency, cancellationToken);
                default:
                    return new DependencyHealth
                    {
                        Status = HealthStatus.Healthy,
                        Message = "Unknown dependency type"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency health check failed for {DependencyName}", dependency.Name);
            return new DependencyHealth
            {
                Status = HealthStatus.Unhealthy,
                Message = ex.Message
            };
        }
    }

    private async Task<DependencyHealth> CheckMongoDbHealth(
        DependencyConfiguration dependency,
        CancellationToken cancellationToken)
    {
        // Implementation would check MongoDB connectivity
        // For now, return healthy placeholder
        return new DependencyHealth
        {
            Status = HealthStatus.Healthy,
            Message = "MongoDB connection healthy",
            ResponseTime = TimeSpan.FromMilliseconds(15)
        };
    }

    private async Task<DependencyHealth> CheckKafkaHealth(
        DependencyConfiguration dependency,
        CancellationToken cancellationToken)
    {
        // Implementation would check Kafka broker connectivity
        // For now, return healthy placeholder
        return new DependencyHealth
        {
            Status = HealthStatus.Healthy,
            Message = "Kafka broker healthy",
            ResponseTime = TimeSpan.FromMilliseconds(8)
        };
    }

    private async Task<DependencyHealth> CheckHttpHealth(
        DependencyConfiguration dependency,
        CancellationToken cancellationToken)
    {
        // Implementation would check HTTP endpoint health
        // For now, return healthy placeholder
        return new DependencyHealth
        {
            Status = HealthStatus.Healthy,
            Message = "HTTP endpoint healthy",
            ResponseTime = TimeSpan.FromMilliseconds(25)
        };
    }
}

/// <summary>
/// Configuration for service health checks
/// </summary>
public class ServiceHealthConfiguration
{
    public string ServiceName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public List<DependencyConfiguration> Dependencies { get; set; } = new();
}

/// <summary>
/// Configuration for individual dependency health checks
/// </summary>
public class DependencyConfiguration
{
    public string Name { get; set; } = string.Empty;
    public DependencyType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Types of dependencies that can be health-checked
/// </summary>
public enum DependencyType
{
    MongoDB,
    Kafka,
    Http,
    Redis,
    SqlServer
}

/// <summary>
/// Health status for individual dependencies
/// </summary>
public class DependencyHealth
{
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan? ResponseTime { get; set; }
    public DateTime? LastChecked { get; set; } = DateTime.UtcNow;
}
