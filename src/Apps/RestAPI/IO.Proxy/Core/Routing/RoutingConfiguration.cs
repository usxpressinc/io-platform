using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace IO.Proxy.Core.Routing;

/// <summary>
/// Centralized routing configuration for IO.Proxy API Gateway
/// Handles routing to downstream microservices with load balancing and path rewriting
/// </summary>
public static class RoutingConfiguration
{
    /// <summary>
    /// Configure API gateway routing with service discovery and load balancing
    /// </summary>
    public static IServiceCollection AddGatewayRouting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure routing settings
        services.Configure<GatewayRoutingSettings>(configuration.GetSection("Gateway:Routing"));

        // Register routing services
        services.AddSingleton<IRouteManager, RouteManager>();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        services.AddSingleton<ILoadBalancer, LoadBalancer>();

        return services;
    }

    /// <summary>
    /// Configure routing middleware
    /// </summary>
    public static IApplicationBuilder UseGatewayRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GatewayRoutingMiddleware>();
    }
}

/// <summary>
/// Gateway routing configuration settings
/// </summary>
public class GatewayRoutingSettings
{
    public Dictionary<string, ServiceRoute> Services { get; set; } = new();
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Service route configuration
/// </summary>
public class ServiceRoute
{
    public string ServiceName { get; set; } = string.Empty;
    public string PathPrefix { get; set; } = string.Empty;
    public List<string> UpstreamHosts { get; set; } = new();
    public int UpstreamPort { get; set; } = 8080;
    public LoadBalancingStrategy LoadBalancingStrategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    public Dictionary<string, string> PathRewrites { get; set; } = new();
    public Dictionary<string, string> HeadersToAdd { get; set; } = new();
    public List<string> HeadersToRemove { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public List<string> RequiredScopes { get; set; } = new();
}

/// <summary>
/// Load balancing strategies
/// </summary>
public enum LoadBalancingStrategy
{
    RoundRobin,
    LeastConnections,
    Random,
    WeightedRoundRobin
}

/// <summary>
/// Interface for managing routes
/// </summary>
public interface IRouteManager
{
    Task<ServiceRoute?> GetRouteAsync(string path);
    Task<bool> IsHealthyAsync(string serviceName);
    void RegisterService(string serviceName, List<string> hosts);
    void UnregisterService(string serviceName);
}

/// <summary>
/// Route manager implementation
/// </summary>
public class RouteManager : IRouteManager
{
    private readonly GatewayRoutingSettings _settings;
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<RouteManager> _logger;
    private readonly Dictionary<string, ServiceRoute> _routeCache;

    public RouteManager(
        GatewayRoutingSettings settings,
        IServiceRegistry serviceRegistry,
        ILogger<RouteManager> logger)
    {
        _settings = settings;
        _serviceRegistry = serviceRegistry;
        _logger = logger;
        _routeCache = new Dictionary<string, ServiceRoute>();

        // Initialize routes from configuration
        InitializeRoutes();
    }

    public async Task<ServiceRoute?> GetRouteAsync(string path)
    {
        // Find matching route based on path prefix
        foreach (var route in _settings.Services.Values)
        {
            if (path.StartsWith(route.PathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Check if service is healthy
                if (await IsHealthyAsync(route.ServiceName))
                {
                    return route;
                }
                else
                {
                    _logger.LogWarning("Service {ServiceName} is unhealthy, skipping route", route.ServiceName);
                }
            }
        }

        return null;
    }

    public async Task<bool> IsHealthyAsync(string serviceName)
    {
        return await _serviceRegistry.IsServiceHealthyAsync(serviceName);
    }

    public void RegisterService(string serviceName, List<string> hosts)
    {
        _serviceRegistry.RegisterService(serviceName, hosts);
        _logger.LogInformation("Registered service {ServiceName} with {HostCount} hosts", serviceName, hosts.Count);
    }

    public void UnregisterService(string serviceName)
    {
        _serviceRegistry.UnregisterService(serviceName);
        _logger.LogInformation("Unregistered service {ServiceName}", serviceName);
    }

    private void InitializeRoutes()
    {
        // Default route configurations for IO Platform services
        _settings.Services = new Dictionary<string, ServiceRoute>
        {
            ["common"] = new ServiceRoute
            {
                ServiceName = "IO.Common",
                PathPrefix = "/api/common",
                UpstreamHosts = new List<string> { "io-common" },
                UpstreamPort = 8081,
                LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                RequireAuthentication = true,
                RequiredScopes = new List<string> { "common" },
                PathRewrites = new Dictionary<string, string>
                {
                    { "^/api/common", "" }
                }
            },
            ["cass"] = new ServiceRoute
            {
                ServiceName = "IO.Cass",
                PathPrefix = "/api/cass",
                UpstreamHosts = new List<string> { "io-cass" },
                UpstreamPort = 8082,
                LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                RequireAuthentication = true,
                RequiredScopes = new List<string> { "clara", "cass" },
                PathRewrites = new Dictionary<string, string>
                {
                    { "^/api/cass", "" }
                }
            },
            ["elsa"] = new ServiceRoute
            {
                ServiceName = "IO.Elsa",
                PathPrefix = "/api/elsa",
                UpstreamHosts = new List<string> { "io-elsa" },
                UpstreamPort = 8083,
                LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                RequireAuthentication = true,
                RequiredScopes = new List<string> { "elsa" },
                PathRewrites = new Dictionary<string, string>
                {
                    { "^/api/elsa", "" }
                }
            },
            ["larry"] = new ServiceRoute
            {
                ServiceName = "IO.Larry",
                PathPrefix = "/api/larry",
                UpstreamHosts = new List<string> { "io-larry" },
                UpstreamPort = 8084,
                LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                RequireAuthentication = true,
                RequiredScopes = new List<string> { "larry" },
                PathRewrites = new Dictionary<string, string>
                {
                    { "^/api/larry", "" }
                }
            },
            ["lea"] = new ServiceRoute
            {
                ServiceName = "IO.Lea",
                PathPrefix = "/api/lea",
                UpstreamHosts = new List<string> { "io-lea" },
                UpstreamPort = 8085,
                LoadBalancingStrategy = LoadBalancingStrategy.RoundRobin,
                RequireAuthentication = true,
                RequiredScopes = new List<string> { "lea" },
                PathRewrites = new Dictionary<string, string>
                {
                    { "^/api/lea", "" }
                }
            }
        };

        _logger.LogInformation("Initialized {RouteCount} routes", _settings.Services.Count);
    }
}

/// <summary>
/// Service registry for tracking available services and their health
/// </summary>
public interface IServiceRegistry
{
    void RegisterService(string serviceName, List<string> hosts);
    void UnregisterService(string serviceName);
    Task<bool> IsServiceHealthyAsync(string serviceName);
    List<string> GetHealthyHosts(string serviceName);
}

/// <summary>
/// Service registry implementation
/// </summary>
public class ServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<string, ServiceInfo> _services;
    private readonly ILogger<ServiceRegistry> _logger;

    public ServiceRegistry(ILogger<ServiceRegistry> logger)
    {
        _services = new Dictionary<string, ServiceInfo>();
        _logger = logger;
    }

    public void RegisterService(string serviceName, List<string> hosts)
    {
        _services[serviceName] = new ServiceInfo
        {
            ServiceName = serviceName,
            Hosts = hosts.Select(host => new HostInfo { Host = host, IsHealthy = true }).ToList(),
            RegisteredAt = DateTime.UtcNow
        };

        _logger.LogInformation("Registered service {ServiceName} with {HostCount} hosts", serviceName, hosts.Count);
    }

    public void UnregisterService(string serviceName)
    {
        if (_services.Remove(serviceName))
        {
            _logger.LogInformation("Unregistered service {ServiceName}", serviceName);
        }
    }

    public async Task<bool> IsServiceHealthyAsync(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var serviceInfo))
        {
            return false;
        }

        // Check if any hosts are healthy
        return serviceInfo.Hosts.Any(host => host.IsHealthy);
    }

    public List<string> GetHealthyHosts(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var serviceInfo))
        {
            return new List<string>();
        }

        return serviceInfo.Hosts
            .Where(host => host.IsHealthy)
            .Select(host => host.Host)
            .ToList();
    }
}

/// <summary>
/// Service information
/// </summary>
public class ServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public List<HostInfo> Hosts { get; set; } = new();
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// Host information
/// </summary>
public class HostInfo
{
    public string Host { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    public int ConsecutiveFailures { get; set; }
}
