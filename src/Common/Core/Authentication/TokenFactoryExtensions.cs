using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace IO.Core.Authentication;

/// <summary>
/// Extension methods for registering TokenFactory services
/// </summary>
public static class TokenFactoryExtensions
{
    /// <summary>
/// Adds TokenFactory services to the dependency injection container
/// </summary>
/// <param name="services">The service collection</param>
/// <param name="configuration">The configuration instance</param>
/// <returns>The service collection for chaining</returns>
public static IServiceCollection AddTokenFactory(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Configure options from configuration/environment variables
    services.Configure<TokenFactoryOptions>(options =>
    {
        options.FactorySecret = configuration["TOKEN_FACTORY__SECRET"] ?? "default-factory-secret-change-in-production";
        options.MasterToken = configuration["TOKEN_FACTORY__MASTER_TOKEN"]; // Master token from env var
        options.MasterTokenSignature = configuration["TOKEN_FACTORY__MASTER_TOKEN_SIGNATURE"];
        options.MasterTokenSecret = configuration["TOKEN_FACTORY__MASTER_TOKEN_SECRET"];
        
        if (int.TryParse(configuration["TOKEN_FACTORY__DEFAULT_LIFETIME_HOURS"], out var hours))
        {
            options.DefaultTokenLifetime = TimeSpan.FromHours(hours);
        }
    });

    // Register TokenFactory as singleton
    services.AddSingleton<TokenFactory>();

    return services;
}

    /// <summary>
    /// Adds TokenFactory services with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTokenFactory(
        this IServiceCollection services,
        Action<TokenFactoryOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<TokenFactory>();

        return services;
    }

    /// <summary>
    /// Adds a token generation endpoint for the TokenFactory
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path for the token generation endpoint</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder MapTokenFactoryEndpoint(
        this IApplicationBuilder builder,
        string path = "/api/token-factory")
    {
        return builder.Map(path, app => app.Run(async (HttpContext context) =>
        {
            if (context.Request.Method != "POST")
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("Only POST method is allowed");
                return;
            }

            try
            {
                // Read the request body
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                var request = System.Text.Json.JsonSerializer.Deserialize<TokenFactoryRequest>(requestBody);

                if (request == null || string.IsNullOrEmpty(request.MasterToken))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Master token is required");
                    return;
                }

                // Get TokenFactory from DI
                var tokenFactory = context.RequestServices.GetRequiredService<TokenFactory>();

                // Generate scoped token
                var result = tokenFactory.GenerateScopedToken(
                    request.MasterToken,
                    request.Scopes ?? Array.Empty<string>(),
                    request.ValidFor,
                    request.RequesterId);

                if (result == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid master token");
                    return;
                }

                // Return the token
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync($"Error generating token: {ex.Message}");
            }
        }));
    }

    /// <summary>
    /// Adds a token validation endpoint for testing
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path for the token validation endpoint</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder MapTokenValidationEndpoint(
        this IApplicationBuilder builder,
        string path = "/api/token-validate")
    {
        return builder.Map(path, app => app.Run(async (HttpContext context) =>
        {
            if (context.Request.Method != "POST")
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("Only POST method is allowed");
                return;
            }

            try
            {
                // Read the request body
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                var request = System.Text.Json.JsonSerializer.Deserialize<TokenValidationRequest>(requestBody);

                if (request == null || string.IsNullOrEmpty(request.Token))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Token is required");
                    return;
                }

                // Get TokenFactory from DI
                var tokenFactory = context.RequestServices.GetRequiredService<TokenFactory>();

                // Validate the token
                var tokenData = tokenFactory.ValidateScopedToken(request.Token);

                if (tokenData == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid or expired token");
                    return;
                }

                // Return token data
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    valid = true,
                    token_id = tokenData.token_id,
                    scopes = tokenData.scopes,
                    expires_at = tokenData.expires_at,
                    generated_at = tokenData.generated_at,
                    requester_id = tokenData.requester_id
                }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync($"Error validating token: {ex.Message}");
            }
        }));
    }
}

/// <summary>
/// Request model for token generation
/// </summary>
public class TokenFactoryRequest
{
    public string MasterToken { get; set; } = string.Empty;
    public string[]? Scopes { get; set; }
    public TimeSpan? ValidFor { get; set; }
    public string? RequesterId { get; set; }
}

/// <summary>
/// Request model for token validation
/// </summary>
public class TokenValidationRequest
{
    public string Token { get; set; } = string.Empty;
}
