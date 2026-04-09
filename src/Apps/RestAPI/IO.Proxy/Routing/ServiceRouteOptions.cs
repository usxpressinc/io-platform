namespace IO.Proxy.Routing;

/// <summary>
/// Configuration options for service routes
/// </summary>
public class ServiceRouteOptions
{
    /// <summary>
    /// Dictionary mapping path prefixes to service route configurations
    /// </summary>
    public Dictionary<string, ServiceRouteConfig> Routes { get; set; } = new();
}
