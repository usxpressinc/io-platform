using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace IO.Proxy.Routing;

/// <summary>
/// Centralized registry for proxy→api routing paths
/// </summary>
public class ServiceRouteRegistry


{
    private readonly Dictionary<string, ServiceRouteConfig> _routes;
    private readonly ILogger<ServiceRouteRegistry> _logger;

    public ServiceRouteRegistry(IOptions<ServiceRouteOptions> options, ILogger<ServiceRouteRegistry> logger)
    {
        this._routes = options.Value.Routes;
        this._logger = logger;
        this.LogConfiguredRoutes();
    }

    private void LogConfiguredRoutes()
    {
        this._logger.LogInformation("ServiceRouteRegistry initialized with {RouteCount} routes", this._routes.Count);
        foreach (var route in this._routes.Select(kvp => kvp.Value))
        {
            this._logger.LogInformation(
                "Route configured: {ServiceName} | PathPrefix: {PathPrefix} | BaseUrl: {BaseUrl} | HttpClient: {HttpClientName} | AuthType: {AuthType}",
                route.ServiceName,
                route.PathPrefix,
                route.BaseUrl,
                route.HttpClientName,
                route.AuthType
            );
        }
    }

    /// <summary>
    /// Finds the service route configuration for a given path
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>The service route configuration, or null if no match is found</returns>
    public ServiceRouteConfig? FindRoute(string path)
    {
        var normalizedPath = path.ToLowerInvariant();

        return (from kvp in this._routes where normalizedPath.StartsWith(kvp.Key, StringComparison.InvariantCultureIgnoreCase) select kvp.Value).FirstOrDefault();

    }

    /// <summary>
    /// Gets a route by its path prefix (exact match)
    /// </summary>
    /// <param name="pathPrefix">The path prefix (e.g., "/api/common")</param>
    /// <returns>The service route configuration, or null if not found</returns>
    public ServiceRouteConfig? GetRouteByPathPrefix(string pathPrefix)
    {
        var normalizedPrefix = pathPrefix.ToLowerInvariant();

        return (from kvp in this._routes where kvp.Key.Equals(normalizedPrefix, StringComparison.InvariantCultureIgnoreCase) select kvp.Value).FirstOrDefault();

    }

    /// <summary>
    /// Gets a route by its service name
    /// </summary>
    /// <param name="serviceName">The service name (e.g., "IO.Common")</param>
    /// <returns>The service route configuration, or null if not found</returns>
    public ServiceRouteConfig? GetRouteByServiceName(string serviceName)
    {
        return (from kvp in this._routes where string.Equals(kvp.Value.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase) select kvp.Value).FirstOrDefault();

    }

    /// <summary>
    /// Gets all registered route keys
    /// </summary>
    public IEnumerable<string> GetRouteKeys() => this._routes.Keys;
}