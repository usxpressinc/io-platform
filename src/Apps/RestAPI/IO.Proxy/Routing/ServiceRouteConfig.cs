namespace IO.Proxy.Routing;

/// <summary>
/// Service route configuration
/// </summary>
public class ServiceRouteConfig
{
    /// <summary>
    /// The logical service name (e.g., "IO.Common")
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// The HttpClient name to use for this service (e.g., "io-common")
    /// </summary>
    public string HttpClientName { get; set; } = string.Empty;

    /// <summary>
    /// The base URL for this service
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The path prefix for this service
    /// </summary>
    public string PathPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Path rewrite rules (regex pattern -> replacement)
    /// </summary>
    public Dictionary<string, string> PathRewrites { get; set; } = new();

    /// <summary>
    /// Headers to add to requests
    /// </summary>
    public Dictionary<string, string> HeadersToAdd { get; set; } = new();

    /// <summary>
    /// Headers to remove from requests
    /// </summary>
    public List<string> HeadersToRemove { get; set; } = [];

    /// <summary>
    /// Query parameters to add to requests
    /// </summary>
    public Dictionary<string, string> QueryParametersToAdd { get; set; } = new();

    /// <summary>
    /// Authentication type for this service
    /// </summary>
    public AuthType AuthType { get; set; } = AuthType.None;

    /// <summary>
    /// OAuth client ID (required when AuthType is OAuth)
    /// </summary>
    public string OAuthClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth resource/audience (required when AuthType is OAuth)
    /// </summary>
    public string OAuthResource { get; set; } = string.Empty;

    /// <summary>
    /// OAuth tenant ID (required when AuthType is OAuth)
    /// </summary>
    public string OAuthTenantId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret (required when AuthType is OAuth)
    /// </summary>
    public string OAuthClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Network credentials username (required when AuthType is NetworkCredentials)
    /// </summary>
    public string NetworkUsername { get; set; } = string.Empty;

    /// <summary>
    /// Network credentials password (required when AuthType is NetworkCredentials)
    /// </summary>
    public string NetworkPassword { get; set; } = string.Empty;

    /// <summary>
    /// Network credentials domain (optional when AuthType is NetworkCredentials)
    /// </summary>
    public string NetworkDomain { get; set; } = string.Empty;
}
