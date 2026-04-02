using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Text;
using System.Text.Json;

namespace IO.Core.Authentication;

/// <summary>
/// Middleware to handle X-Auth token authentication for the IO Proxy
/// </summary>
public class XAuthTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<XAuthTokenMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly XAuthTokenOptions _options;
    private readonly TokenFactory? _tokenFactory;
    private HttpContext _httpContext;

    public XAuthTokenMiddleware(
        RequestDelegate next,
        ILogger<XAuthTokenMiddleware> logger,
        IHttpClientFactory httpClientFactory,
        XAuthTokenOptions options,
        TokenFactory? tokenFactory = null)
    {
        this._next = next;
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._options = options;
        this._tokenFactory = tokenFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        this._httpContext = context;
        
        // Skip authentication for health endpoints and Swagger
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await this._next(context);
            return;
        }

        var token = this.ExtractToken(context.Request);
        
        if (string.IsNullOrEmpty(token))
        {
            this._logger.LogWarning("Missing X-Auth token for request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(CreateErrorResponse("missing_token", "X-Auth token is required"));
            return;
        }

        var validationResult = await this.ValidateTokenAsync(token);
        
        if (!validationResult.IsValid)
        {
            this._logger.LogWarning("Invalid X-Auth token for request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(CreateErrorResponse(
                validationResult.ErrorCode ?? "invalid_token", 
                validationResult.ErrorMessage ?? "X-Auth token is invalid or expired"));
            return;
        }

        // Add token and user info to context for downstream use
        context.Items["XAuthToken"] = token;
        context.Items["UserId"] = validationResult.UserId;
        context.Items["Scopes"] = validationResult.Scopes;
        
        await this._next(context);
    }

    private string? ExtractToken(HttpRequest request)
    {
        // Try Authorization header first (Bearer token)
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Try X-Auth-Token header
        if (request.Headers.TryGetValue("X-Auth-Token", out var xAuthToken))
        {
            return xAuthToken.FirstOrDefault();
        }

        return null;
    }

    private async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            // Get the required scopes from the endpoint
            var requiredScopes = this.GetRequiredScopesForRequest();
            
            if (requiredScopes.Length == 0)
            {
                // No scopes required, allow access
                return new TokenValidationResult { IsValid = true };
            }
            
            // Use MSAL to validate the token and extract scopes
            return await this.ValidateTokenWithMSALAsync(token, requiredScopes);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error validating X-Auth token");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "validation_error",
                ErrorMessage = "Token validation failed"
            };
        }
    }

    private async Task<TokenValidationResult> ValidateTokenWithMSALAsync(string token, string[] requiredScopes)
    {
        try
        {
            // Option 1: Simple token validation (for development/testing)
            if (token == this._options.ApiToken)
            {
                return new TokenValidationResult 
                { 
                    IsValid = true, 
                    UserId = "api-user",
                    Scopes = ["common", "clara", "elsa", "larry", "lea"]
                };
            }

            // Option 2: Validate scoped token with TokenFactory
            if (this._tokenFactory != null && token.Contains('.'))
            {
                return await this.ValidateScopedTokenAsync(token, requiredScopes);
            }

            // Option 3: Use MSAL to validate JWT token
            if (token.StartsWith("eyJ"))
            {
                return await this.ValidateJWTTokenWithMSALAsync(token, requiredScopes);
            }

            // Option 4: Call external validation service
            if (!string.IsNullOrEmpty(this._options.ValidationEndpoint))
            {
                return await this.ValidateTokenWithServiceAsync(token, requiredScopes);
            }

            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "invalid_token",
                ErrorMessage = "Token format not supported"
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error in MSAL token validation");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "validation_error",
                ErrorMessage = "Token validation failed"
            };
        }
    }

    private async Task<TokenValidationResult> ValidateScopedTokenAsync(string token, string[] requiredScopes)
    {
        try
        {
            if (this._tokenFactory == null)
            {
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorCode = "factory_unavailable",
                    ErrorMessage = "Token factory not available"
                };
            }

            var tokenData = this._tokenFactory.ValidateScopedToken(token);
            
            if (tokenData == null)
            {
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorCode = "invalid_scoped_token",
                    ErrorMessage = "Invalid or expired scoped token"
                };
            }

            // Check if token has any of the required scopes
            var hasRequiredScope = requiredScopes.Any(scope => tokenData.scopes.Contains(scope));

            return new TokenValidationResult
            {
                IsValid = hasRequiredScope,
                UserId = tokenData.requester_id,
                Scopes = tokenData.scopes,
                ErrorCode = hasRequiredScope ? null : "insufficient_scope",
                ErrorMessage = hasRequiredScope ? null : "Token lacks required scopes"
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error validating scoped token");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "validation_error",
                ErrorMessage = "Scoped token validation failed"
            };
        }
    }

    private async Task<TokenValidationResult> ValidateJWTTokenWithMSALAsync(string token, string[] requiredScopes)
    {
        try
        {
            // Create MSAL confidential client application
            var app = ConfidentialClientApplicationBuilder
                .Create(this._options.ClientId!)
                .WithCertificate(this._options.Certificate) // Use certificate or secret
                .WithAuthority(new Uri(this._options.Authority!))
                .Build();

            // Validate the token and extract claims
            var result = await app.AcquireTokenForClient(requiredScopes)
                .WithExtraQueryParameters(new Dictionary<string, string>
                {
                    ["access_token"] = token
                })
                .ExecuteAsync();

            if (result == null)
            {
                return new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorCode = "invalid_token",
                    ErrorMessage = "Failed to acquire token"
                };
            }

            // Extract user information and scopes from the validated token
            var tokenScopes = result.Scopes?.ToArray() ?? [
            ];
            var userId = result.Account?.HomeAccountId?.Identifier ?? "unknown";

            // Check if token has any of the required scopes
            var hasRequiredScope = requiredScopes.Any(scope => tokenScopes.Contains(scope));

            return new TokenValidationResult
            {
                IsValid = hasRequiredScope,
                UserId = userId,
                Scopes = tokenScopes,
                ErrorCode = hasRequiredScope ? null : "insufficient_scope",
                ErrorMessage = hasRequiredScope ? null : "Token lacks required scopes"
            };
        }
        catch (MsalServiceException ex)
        {
            this._logger.LogError(ex, "MSAL service exception: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = ex.ErrorCode.ToString(),
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Unexpected error in MSAL validation");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "validation_error",
                ErrorMessage = "Unexpected validation error"
            };
        }
    }

    private async Task<TokenValidationResult> ValidateTokenWithServiceAsync(string token, string[] requiredScopes)
    {
        try
        {
            var client = this._httpClientFactory.CreateClient("token-validation");
            
            var request = new HttpRequestMessage(HttpMethod.Post, this._options.ValidationEndpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { 
                        token = token,
                        required_scopes = requiredScopes
                    }),
                    Encoding.UTF8,
                    "application/json")
            };
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this._options.ValidationToken);
            
            var response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TokenValidationResult>(content);
                return result ?? new TokenValidationResult 
                { 
                    IsValid = false, 
                    ErrorCode = "invalid_response",
                    ErrorMessage = "Invalid response from validation service"
                };
            }
            
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "service_error",
                ErrorMessage = $"Validation service returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error calling token validation service");
            return new TokenValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "service_error",
                ErrorMessage = "Validation service unavailable"
            };
        }
    }

    private string[] GetRequiredScopesForRequest()
    {
        // This would normally be extracted from the endpoint's authorization attributes
        // For now, we'll implement a simple path-based scope mapping
        var path = this._httpContext?.Request?.Path.Value ?? "";
        
        return path switch
        {
            var p when p.StartsWith("/api/common/") => new[] { "common" },
            var p when p.StartsWith("/api/clara/") => new[] { "clara" }, // Python uses "clara" for CASS
            var p when p.StartsWith("/api/cass/") => new[] { "clara" },
            var p when p.StartsWith("/api/elsa/") => new[] { "elsa" },
            var p when p.StartsWith("/api/larry/") => new[] { "larry" },
            var p when p.StartsWith("/api/lea/") => new[] { "lea" },
            _ => Array.Empty<string>()
        };
    }

    private static bool ShouldSkipAuthentication(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Skip authentication for health endpoints
        if (path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/ready", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Skip authentication for Swagger/OpenAPI endpoints
        if (path.Contains("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string CreateErrorResponse(string code, string description)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            code = code,
            description = description,
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Configuration options for X-Auth token middleware
/// </summary>
public class XAuthTokenOptions
{
    public string? ApiToken { get; set; }
    public string? ValidationEndpoint { get; set; }
    public string? ValidationToken { get; set; }
    public string? JwtSecret { get; set; }
    
    // MSAL Configuration
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Authority { get; set; }
    public string? TenantId { get; set; }
    public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificate { get; set; }
}

/// <summary>
/// Result from token validation service
/// </summary>
internal class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string[]? Scopes { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
