using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace IO.Proxy.Routing;

/// <summary>
/// Base auth handler for HTTP clients
/// </summary>
public abstract class AuthHandler : DelegatingHandler
{
    protected readonly ILogger _logger;

    protected AuthHandler(ILogger logger)
    {
        this._logger = logger;
    }
}

/// <summary>
/// OAuth authentication handler for HTTP clients using MSAL
/// </summary>
public class OAuthAuthHandler : AuthHandler
{
    private readonly string _clientId;
    private readonly string _resource;
    private readonly IConfidentialClientApplication _msalClient;

    public OAuthAuthHandler(
        string clientId,
        string resource,
        string tenantId,
        string clientSecret,
        ILogger<OAuthAuthHandler> logger)
        : base(logger)
    {
        this._clientId = clientId;
        this._resource = resource;

        this._msalClient = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Acquire token using MSAL
            var scopes = new[] { $"{this._resource}/.default" };
            var result = await this._msalClient
                .AcquireTokenForClient(scopes)
                .ExecuteAsync(cancellationToken);

            // Add Bearer token to request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            this._logger.LogDebug(
                "Added OAuth token for client: {ClientId}, resource: {Resource}, expiresOn: {ExpiresOn}", this._clientId, this._resource,
                result.ExpiresOn
            );
        }
        catch (MsalServiceException ex)
        {
            this._logger.LogError(ex, "Failed to acquire OAuth token for client: {ClientId}, resource: {Resource}", this._clientId, this._resource);
            throw;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Factory for creating OAuth auth handlers with service-specific credentials
/// </summary>
public class OAuthAuthHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OAuthAuthHandlerFactory(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public OAuthAuthHandler CreateHandler(string pathPrefix)
    {
        var routeRegistry = this._serviceProvider.GetRequiredService<ServiceRouteRegistry>();
        var route = routeRegistry.FindRoute(pathPrefix);

        if (route == null || route.AuthType != AuthType.OAuth)
        {
            throw new InvalidOperationException($"No OAuth configuration found for path: {pathPrefix}");
        }

        var logger = this._serviceProvider.GetRequiredService<ILogger<OAuthAuthHandler>>();
        return new OAuthAuthHandler(
            route.OAuthClientId,
            route.OAuthResource,
            route.OAuthTenantId,
            route.OAuthClientSecret,
            logger
        );
    }
}

/// <summary>
/// Network credentials authentication handler for HTTP clients
/// </summary>
public class NetworkCredentialsAuthHandler : AuthHandler
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _domain;

    public NetworkCredentialsAuthHandler(string username, string password, string domain, ILogger<NetworkCredentialsAuthHandler> logger)
        : base(logger)
    {
        this._username = username;
        this._password = password;
        this._domain = domain;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Add basic auth header for network credentials
        var credentials = string.IsNullOrEmpty(this._domain)
            ? $"{this._username}:{this._password}"
            : $"{this._domain}\\{this._username}:{this._password}";

        var encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        this._logger.LogDebug("Adding Network Credentials auth for user: {Username}, domain: {Domain}", this._username, this._domain);

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Factory for creating NetworkCredentials auth handlers with service-specific credentials
/// </summary>
public class NetworkCredentialsAuthHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public NetworkCredentialsAuthHandlerFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        this._serviceProvider = serviceProvider;
        this._configuration = configuration;
    }

    public NetworkCredentialsAuthHandler CreateHandler(string pathPrefix)
    {
        var routeRegistry = this._serviceProvider.GetRequiredService<ServiceRouteRegistry>();
        var route = routeRegistry.FindRoute(pathPrefix);

        if (route == null || route.AuthType != AuthType.NetworkCredentials)
        {
            throw new InvalidOperationException($"No NetworkCredentials configuration found for path: {pathPrefix}");
        }

        var logger = this._serviceProvider.GetRequiredService<ILogger<NetworkCredentialsAuthHandler>>();
        
        // Pull credentials from configuration if not specified in route config
        var username = string.IsNullOrEmpty(route.NetworkUsername)
            ? this._configuration["SERVICE_ACCOUNT_USER"] ?? string.Empty
            : route.NetworkUsername;
        
        var password = string.IsNullOrEmpty(route.NetworkPassword)
            ? this._configuration["SERVICE_ACCOUNT_PASSWORD"] ?? string.Empty
            : route.NetworkPassword;
        
        var domain = string.IsNullOrEmpty(route.NetworkDomain)
            ? "USXENT"
            : route.NetworkDomain;

        return new NetworkCredentialsAuthHandler(username, password, domain, logger);
    }
}
