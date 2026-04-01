using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;

namespace IO.Core.Authentication;

/// <summary>
/// Extension methods for registering X-Auth token middleware
/// </summary>
public static class XAuthTokenExtensions
{
    /// <summary>
    /// Adds X-Auth token middleware services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddXAuthTokenAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<XAuthTokenOptions>(options =>
        {
            options.ApiToken = configuration["AUTH:API_TOKEN"];
            options.ValidationEndpoint = configuration["AUTH:VALIDATION_ENDPOINT"];
            options.ValidationToken = configuration["AUTH:VALIDATION_TOKEN"];
            options.JwtSecret = configuration["AUTH:JWT_SECRET"];
        });

        // Register HTTP client for token validation
        if (!string.IsNullOrEmpty(configuration["AUTH:VALIDATION_ENDPOINT"]))
        {
            services.AddHttpClient("token-validation", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
            });
        }

        return services;
    }

    /// <summary>
    /// Adds X-Auth token middleware services with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddXAuthTokenAuthentication(
        this IServiceCollection services,
        Action<XAuthTokenOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Register HTTP client for token validation
        services.AddHttpClient("token-validation", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
        });

        return services;
    }

    /// <summary>
    /// Adds the X-Auth token middleware to the request pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseXAuthTokenAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<XAuthTokenMiddleware>();
    }

    /// <summary>
    /// Adds the X-Auth token middleware with custom options
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseXAuthTokenAuthentication(
        this IApplicationBuilder builder,
        Action<XAuthTokenOptions> configureOptions)
    {
        // Configure options inline
        var options = new XAuthTokenOptions();
        configureOptions(options);
        
        // Create a temporary service provider to get the logger and HTTP client factory
        using var scope = builder.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        return builder.UseMiddleware<XAuthTokenMiddleware>(logger, httpClientFactory, options);
    }
}
