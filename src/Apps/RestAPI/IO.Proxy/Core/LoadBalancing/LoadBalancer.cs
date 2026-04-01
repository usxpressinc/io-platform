using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IO.Proxy.Core.LoadBalancing;

/// <summary>
/// Load balancer for distributing requests across multiple service instances
/// Supports multiple load balancing strategies and health checking
/// </summary>
public interface ILoadBalancer
{
    string? SelectHost(string serviceName, LoadBalancingStrategy strategy);
    void UpdateHostHealth(string serviceName, string host, bool isHealthy);
    void RegisterHosts(string serviceName, List<string> hosts);
    void UnregisterHosts(string serviceName);
}

/// <summary>
/// Load balancer implementation
/// </summary>
public class LoadBalancer : ILoadBalancer
{
    private readonly ConcurrentDictionary<string, ServiceLoadBalancer> _services;
    private readonly ILogger<LoadBalancer> _logger;

    public LoadBalancer(ILogger<LoadBalancer> logger)
    {
        _services = new ConcurrentDictionary<string, ServiceLoadBalancer>();
        _logger = logger;
    }

    public string? SelectHost(string serviceName, LoadBalancingStrategy strategy)
    {
        if (!_services.TryGetValue(serviceName, out var serviceBalancer))
        {
            _logger.LogWarning("Service {ServiceName} not found in load balancer", serviceName);
            return null;
        }

        return serviceBalancer.SelectHost(strategy);
    }

    public void UpdateHostHealth(string serviceName, string host, bool isHealthy)
    {
        if (_services.TryGetValue(serviceName, out var serviceBalancer))
        {
            serviceBalancer.UpdateHostHealth(host, isHealthy);
        }
    }

    public void RegisterHosts(string serviceName, List<string> hosts)
    {
        _services.AddOrUpdate(serviceName,
            new ServiceLoadBalancer(serviceName, hosts, _logger),
            (key, existing) => existing.UpdateHosts(hosts));

        _logger.LogInformation("Registered {HostCount} hosts for service {ServiceName}", hosts.Count, serviceName);
    }

    public void UnregisterHosts(string serviceName)
    {
        if (_services.TryRemove(serviceName, out _))
        {
            _logger.LogInformation("Unregistered service {ServiceName} from load balancer", serviceName);
        }
    }
}

/// <summary>
/// Per-service load balancer
/// </summary>
public class ServiceLoadBalancer
{
    private readonly string _serviceName;
    private readonly ConcurrentDictionary<string, HostState> _hosts;
    private readonly object _roundRobinLock = new();
    private readonly object _weightedLock = new();
    private readonly ILogger _logger;

    private int _roundRobinIndex = 0;

    public ServiceLoadBalancer(string serviceName, List<string> hosts, ILogger logger)
    {
        _serviceName = serviceName;
        _hosts = new ConcurrentDictionary<string, HostState>(
            hosts.ToDictionary(host => host, host => new HostState(host)));
        _logger = logger;
    }

    public string? SelectHost(LoadBalancingStrategy strategy)
    {
        var healthyHosts = _hosts.Values
            .Where(host => host.IsHealthy)
            .ToList();

        if (!healthyHosts.Any())
        {
            _logger.LogWarning("No healthy hosts available for service {ServiceName}", _serviceName);
            return null;
        }

        return strategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectRoundRobin(healthyHosts),
            LoadBalancingStrategy.LeastConnections => SelectLeastConnections(healthyHosts),
            LoadBalancingStrategy.Random => SelectRandom(healthyHosts),
            LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobin(healthyHosts),
            _ => healthyHosts.First().Host
        };
    }

    public void UpdateHostHealth(string host, bool isHealthy)
    {
        if (_hosts.TryGetValue(host, out var hostState))
        {
            hostState.UpdateHealth(isHealthy);
            _logger.LogDebug(
                "Updated health for host {Host} of service {ServiceName}: {IsHealthy}",
                host, _serviceName, isHealthy);
        }
    }

    public ServiceLoadBalancer UpdateHosts(List<string> hosts)
    {
        // Add new hosts
        foreach (var host in hosts)
        {
            _hosts.TryAdd(host, new HostState(host));
        }

        // Remove hosts that are no longer in the list
        foreach (var existingHost in _hosts.Keys.ToList())
        {
            if (!hosts.Contains(existingHost))
            {
                _hosts.TryRemove(existingHost, out _);
            }
        }

        return this;
    }

    private string SelectRoundRobin(List<HostState> healthyHosts)
    {
        lock (_roundRobinLock)
        {
            var host = healthyHosts[_roundRobinIndex % healthyHosts.Count];
            _roundRobinIndex++;
            return host.Host;
        }
    }

    private string SelectLeastConnections(List<HostState> healthyHosts)
    {
        return healthyHosts
            .OrderBy(host => host.ActiveConnections)
            .ThenBy(host => host.Host)
            .First()
            .Host;
    }

    private string SelectRandom(List<HostState> healthyHosts)
    {
        var random = new Random();
        var index = random.Next(healthyHosts.Count);
        return healthyHosts[index].Host;
    }

    private string SelectWeightedRoundRobin(List<HostState> healthyHosts)
    {
        lock (_weightedLock)
        {
            // Simple weighted round robin based on host health score
            var weightedHosts = healthyHosts
                .SelectMany(host => Enumerable.Repeat(host, Math.Max(1, host.HealthScore)))
                .ToList();

            if (!weightedHosts.Any())
            {
                return healthyHosts.First().Host;
            }

            var index = _roundRobinIndex % weightedHosts.Count;
            _roundRobinIndex++;
            return weightedHosts[index].Host;
        }
    }
}

/// <summary>
/// Host state information
/// </summary>
public class HostState
{
    public string Host { get; }
    public bool IsHealthy { get; private set; }
    public int ActiveConnections { get; private set; }
    public int HealthScore { get; private set; }
    public DateTime LastHealthCheck { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTime? LastFailure { get; private set; }

    public HostState(string host)
    {
        Host = host;
        IsHealthy = true;
        ActiveConnections = 0;
        HealthScore = 100;
        LastHealthCheck = DateTime.UtcNow;
        ConsecutiveFailures = 0;
    }

    public void UpdateHealth(bool isHealthy)
    {
        var now = DateTime.UtcNow;
        LastHealthCheck = now;

        if (isHealthy)
        {
            if (!IsHealthy)
            {
                // Host recovered
                ConsecutiveFailures = 0;
                HealthScore = Math.Min(100, HealthScore + 10);
                _logger.LogInformation("Host {Host} recovered and marked as healthy", Host);
            }
            else
            {
                // Gradually improve health score
                HealthScore = Math.Min(100, HealthScore + 1);
            }
        }
        else
        {
            ConsecutiveFailures++;
            LastFailure = now;
            HealthScore = Math.Max(0, HealthScore - 20);

            // Mark as unhealthy after consecutive failures
            if (ConsecutiveFailures >= 3)
            {
                IsHealthy = false;
                _logger.LogWarning("Host {Host} marked as unhealthy after {Failures} consecutive failures", 
                    Host, ConsecutiveFailures);
            }
        }

        IsHealthy = isHealthy && ConsecutiveFailures < 3;
    }

    public void IncrementConnections()
    {
        ActiveConnections++;
    }

    public void DecrementConnections()
    {
        ActiveConnections = Math.Max(0, ActiveConnections - 1);
    }
}

/// <summary>
/// Load balancing metrics
/// </summary>
public class LoadBalancingMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public int TotalHosts { get; set; }
    public int HealthyHosts { get; set; }
    public int UnhealthyHosts { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, int> HostRequestCounts { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Load balancer metrics collector
/// </summary>
public class LoadBalancerMetrics
{
    private readonly ILoadBalancer _loadBalancer;
    private readonly ConcurrentDictionary<string, LoadBalancingMetrics> _metrics;
    private readonly ILogger<LoadBalancerMetrics> _logger;

    public LoadBalancerMetrics(ILoadBalancer loadBalancer, ILogger<LoadBalancerMetrics> logger)
    {
        _loadBalancer = loadBalancer;
        _metrics = new ConcurrentDictionary<string, LoadBalancingMetrics>();
        _logger = logger;
    }

    public void RecordRequest(string serviceName, string host, TimeSpan responseTime)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new LoadBalancingMetrics
        {
            ServiceName = serviceName
        });

        metrics.TotalRequests++;
        metrics.HostRequestCounts.AddOrUpdate(host, 1, (_, count) => count + 1);
        
        // Update average response time (simple moving average)
        var newAvg = (metrics.AverageResponseTime * (metrics.TotalRequests - 1) + responseTime.TotalMilliseconds) / metrics.TotalRequests;
        metrics.AverageResponseTime = newAvg;
        metrics.LastUpdated = DateTime.UtcNow;
    }

    public LoadBalancingMetrics? GetMetrics(string serviceName)
    {
        return _metrics.TryGetValue(serviceName, out var metrics) ? metrics : null;
    }

    public Dictionary<string, LoadBalancingMetrics> GetAllMetrics()
    {
        return new Dictionary<string, LoadBalancingMetrics>(_metrics);
    }
}
