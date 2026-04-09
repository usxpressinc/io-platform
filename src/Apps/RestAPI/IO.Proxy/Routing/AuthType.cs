namespace IO.Proxy.Routing;

/// <summary>
/// Authentication types for service routes
/// </summary>
public enum AuthType
{
    /// <summary>
    /// No authentication
    /// </summary>
    None,

    /// <summary>
    /// OAuth authentication with clientId and resource
    /// </summary>
    OAuth,

    /// <summary>
    /// Network credentials (username/password)
    /// </summary>
    NetworkCredentials
}
