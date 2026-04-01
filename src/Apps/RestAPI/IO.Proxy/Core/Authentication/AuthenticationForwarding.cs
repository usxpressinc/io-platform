using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace IO.Proxy.Core.Authentication;

/// <summary>
/// Authentication forwarding service for API Gateway
/// Handles token validation, scope checking, and authentication forwarding to downstream services
/// </summary>
public interface IAuthenticationForwardingService
{
    Task<bool> ValidateRequestAsync(HttpContext context);
    Task ForwardAuthenticationAsync(HttpContext context, HttpRequestMessage downstreamRequest);
    ClaimsPrincipal? GetUserFromContext(HttpContext context);
    bool HasRequiredScope(HttpContext context, string requiredScope);
}

/// <summary>
/// Authentication forwarding service implementation
/// </summary>
public class AuthenticationForwardingService : IAuthenticationForwardingService
{
    private readonly ILogger<AuthenticationForwardingService> _logger;
    private readonly AuthenticationSettings _settings;

    public AuthenticationForwardingService(
        ILogger<AuthenticationForwardingService> logger,
        AuthenticationSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task<bool> ValidateRequestAsync(HttpContext context)
    {
        var token = ExtractToken(context);
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Missing authentication token for request: {Path}", context.Request.Path);
            return false;
        }

        // Validate token (this would integrate with existing XAuthTokenFactory)
        var validationResult = await ValidateTokenAsync(token, context);
        
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid authentication token for request: {Path} - {Error}", 
                context.Request.Path, validationResult.ErrorMessage);
            return false;
        }

        // Store user information in context
        context.User = CreateClaimsPrincipal(validationResult);
        context.Items["UserScopes"] = validationResult.Scopes;
        context.Items["UserId"] = validationResult.UserId;
        context.Items["Token"] = token;

        return true;
    }

    public async Task ForwardAuthenticationAsync(HttpContext context, HttpRequestMessage downstreamRequest)
    {
        var token = context.Items["Token"]?.ToString();
        
        if (!string.IsNullOrEmpty(token))
        {
            // Forward original token
            downstreamRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Add X-Auth-Token header for downstream services
        if (!string.IsNullOrEmpty(token))
        {
            downstreamRequest.Headers.Add("X-Auth-Token", token);
        }

        // Add user context headers
        if (context.Items["UserId"] is string userId)
        {
            downstreamRequest.Headers.Add("X-User-Id", userId);
        }

        // Add scope information
        if (context.Items["UserScopes"] is List<string> scopes && scopes.Any())
        {
            downstreamRequest.Headers.Add("X-User-Scopes", string.Join(",", scopes));
        }

        // Add request tracing information
        downstreamRequest.Headers.Add("X-Request-Id", context.TraceIdentifier);
        downstreamRequest.Headers.Add("X-Original-Host", context.Request.Host.ToString());
        downstreamRequest.Headers.Add("X-Original-Path", context.Request.Path.ToString());

        _logger.LogDebug("Forwarded authentication for downstream request to {Path}", 
            downstreamRequest.RequestUri?.PathAndQuery);
    }

    public ClaimsPrincipal? GetUserFromContext(HttpContext context)
    {
        return context.User;
    }

    public bool HasRequiredScope(HttpContext context, string requiredScope)
    {
        if (context.Items["UserScopes"] is not List<string> scopes)
        {
            return false;
        }

        return scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase);
    }

    private string? ExtractToken(HttpContext context)
    {
        // Try Authorization header first (Bearer token)
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader != null && authHeader.Scheme?.Equals("Bearer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return authHeader.Parameter;
        }

        // Try X-Auth-Token header
        if (context.Request.Headers.TryGetValue("X-Auth-Token", out var xAuthToken))
        {
            return xAuthToken.FirstOrDefault();
        }

        return null;
    }

    private async Task<TokenValidationResult> ValidateTokenAsync(string token, HttpContext context)
    {
        try
        {
            // Get required scopes for the requested endpoint
            var requiredScopes = GetRequiredScopesForEndpoint(context.Request.Path);

            // This would integrate with the existing XAuthTokenFactory
            // For now, implement a simple validation
            if (token == _settings.MasterToken)
            {
                return new TokenValidationResult
                {
                    IsValid = true,
                    UserId = "system",
                    Scopes = new List<string> { "common", "clara", "elsa", "larry", "lea" },
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };
            }

            // For development/testing, accept simple tokens
            if (token.StartsWith("dev-") && _settings.IsDevelopment)
            {
                var userId = token.Substring(4);
                return new TokenValidationResult
                {
                    IsValid = true,
                    UserId = userId,
                    Scopes = new List<string> { "common", "clara", "elsa", "larry", "lea" },
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };
            }

            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }

    private List<string> GetRequiredScopesForEndpoint(string path)
    {
        return path.ToLowerInvariant() switch
        {
            var p when p.StartsWith("/api/common/") => new List<string> { "common" },
            var p when p.StartsWith("/api/cass/") => new List<string> { "clara", "cass" },
            var p when p.StartsWith("/api/elsa/") => new List<string> { "elsa" },
            var p when p.StartsWith("/api/larry/") => new List<string> { "larry" },
            var p when p.StartsWith("/api/lea/") => new List<string> { "lea" },
            _ => new List<string>()
        };
    }

    private ClaimsPrincipal CreateClaimsPrincipal(TokenValidationResult validationResult)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, validationResult.UserId),
            new Claim(ClaimTypes.Name, validationResult.UserId),
            new Claim("sub", validationResult.UserId),
            new Claim("preferred_username", validationResult.UserId)
        };

        // Add scope claims
        foreach (var scope in validationResult.Scopes)
        {
            claims.Add(new Claim("scope", scope));
            claims.Add(new Claim("roles", scope));
        }

        // Add token expiration
        if (validationResult.ExpiresAt.HasValue)
        {
            claims.Add(new Claim("exp", new DateTimeOffset(validationResult.ExpiresAt.Value).ToUnixTimeSeconds().ToString()));
        }

        var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Authentication settings
/// </summary>
public class AuthenticationSettings
{
    public string MasterToken { get; set; } = string.Empty;
    public bool IsDevelopment { get; set; } = false;
    public bool RequireAuthentication { get; set; } = true;
    public List<string> PublicPaths { get; set; } = new();
    public TimeSpan TokenValidationCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Authentication forwarding middleware
/// </summary>
public class AuthenticationForwardingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthenticationForwardingService _authService;
    private readonly ILogger<AuthenticationForwardingMiddleware> _logger;

    public AuthenticationForwardingMiddleware(
        RequestDelegate next,
        IAuthenticationForwardingService authService,
        ILogger<AuthenticationForwardingMiddleware> logger)
    {
        _next = next;
        _authService = authService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for public paths
        if (IsPublicPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Validate authentication
        var isValid = await _authService.ValidateRequestAsync(context);
        
        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(CreateErrorResponse("authentication_required", "Valid authentication token is required"));
            return;
        }

        // Check scope requirements
        var requiredScope = GetRequiredScopeForEndpoint(context.Request.Path);
        if (!string.IsNullOrEmpty(requiredScope) && !_authService.HasRequiredScope(context, requiredScope))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(CreateErrorResponse("insufficient_scope", $"Required scope '{requiredScope}' not found"));
            return;
        }

        await _next(context);
    }

    private bool IsPublicPath(string path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/ready",
            "/metrics",
            "/swagger",
            "/openapi"
        };

        return publicPaths.Any(publicPath => 
            path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase));
    }

    private string? GetRequiredScopeForEndpoint(string path)
    {
        return path.ToLowerInvariant() switch
        {
            var p when p.StartsWith("/api/common/") => "common",
            var p when p.StartsWith("/api/cass/") => "clara",
            var p when p.StartsWith("/api/elsa/") => "elsa",
            var p when p.StartsWith("/api/larry/") => "larry",
            var p when p.StartsWith("/api/lea/") => "lea",
            _ => null
        };
    }

    private string CreateErrorResponse(string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            code = code,
            message = message,
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Extension methods for authentication forwarding
/// </summary>
public static class AuthenticationForwardingExtensions
{
    /// <summary>
    /// Adds authentication forwarding services
    /// </summary>
    public static IServiceCollection AddAuthenticationForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthenticationSettings>(configuration.GetSection("Gateway:Authentication"));
        services.AddSingleton<IAuthenticationForwardingService, AuthenticationForwardingService>();

        return services;
    }

    /// <summary>
    /// Adds authentication forwarding middleware
    /// </summary>
    public static IApplicationBuilder UseAuthenticationForwarding(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationForwardingMiddleware>();
    }
}
