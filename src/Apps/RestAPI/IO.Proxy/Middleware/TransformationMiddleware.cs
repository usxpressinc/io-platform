using System.Text.Json;
using System.Text.RegularExpressions;

namespace IO.Proxy.Middleware;

/// <summary>
/// Request/response transformation middleware for API Gateway
/// Handles path rewriting, header management, and content transformation
/// </summary>
public class TransformationMiddleware(
    RequestDelegate next,
    ILogger<TransformationMiddleware> logger,
    TransformationSettings settings
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var originalPath = context.Request.Path.Value;
        var originalQueryString = context.Request.QueryString.Value;

        try
        {
            // Transform request
            await this.TransformRequestAsync(context);

            // Process the request
            await next(context);

            // Transform response
            await this.TransformResponseAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during request/response transformation");
            throw;
        }
        finally
        {
            // Log the transformation for debugging
            if (settings.EnableTransformationLogging)
            {
                logger.LogDebug(
                    "Request transformed: {OriginalPath} -> {NewPath} | {OriginalQS} -> {NewQS}",
                    originalPath,
                    context.Request.Path.Value,
                    originalQueryString,
                    context.Request.QueryString.Value
                );
            }
        }
    }

    private async Task TransformRequestAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var serviceRoute = this.FindServiceRoute(path);

        if (serviceRoute != null)
        {
            // Apply path rewriting
            await this.ApplyPathRewritingAsync(context, serviceRoute);

            // Add headers
            this.AddHeaders(context, serviceRoute);

            // Remove headers
            this.RemoveHeaders(context, serviceRoute);

            // Store service information for downstream use
            context.Items["ServiceName"] = serviceRoute.ServiceName;
            context.Items["OriginalPath"] = path;
        }
    }

    private async Task TransformResponseAsync(HttpContext context)
    {
        // Add gateway-specific response headers
        if (settings.AddGatewayHeaders)
        {
            context.Response.Headers.Append(
                "X-Gateway-Service",
                context.Items["ServiceName"]?.ToString() ?? "unknown"
            );
            context.Response.Headers.Append("X-Gateway-Version", "1.0.0");
            context.Response.Headers.Append(
                "X-Gateway-Timestamp",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );
        }

        // Transform response body if needed
        if (settings.TransformResponseBody)
        {
            await this.TransformResponseBodyAsync(context);
        }
    }

    private ServiceRoute? FindServiceRoute(string path)
    {
        // This would typically come from the route manager
        // For now, implement basic path matching
        return path.ToLowerInvariant() switch
        {
            var p when p.StartsWith("/api/common/") => new ServiceRoute
            {
                ServiceName = "IO.Common",
                PathPrefix = "/api/common",
                PathRewrites = new Dictionary<string, string> { { "^/api/common", "" } },
            },
            var p when p.StartsWith("/api/cass/") => new ServiceRoute
            {
                ServiceName = "IO.Cass",
                PathPrefix = "/api/cass",
                PathRewrites = new Dictionary<string, string> { { "^/api/cass", "" } },
            },
            var p when p.StartsWith("/api/elsa/") => new ServiceRoute
            {
                ServiceName = "IO.Elsa",
                PathPrefix = "/api/elsa",
                PathRewrites = new Dictionary<string, string> { { "^/api/elsa", "" } },
            },
            var p when p.StartsWith("/api/larry/") => new ServiceRoute
            {
                ServiceName = "IO.Larry",
                PathPrefix = "/api/larry",
                PathRewrites = new Dictionary<string, string> { { "^/api/larry", "" } },
            },
            var p when p.StartsWith("/api/lea/") => new ServiceRoute
            {
                ServiceName = "IO.Lea",
                PathPrefix = "/api/lea",
                PathRewrites = new Dictionary<string, string> { { "^/api/lea", "" } },
            },
            _ => null,
        };
    }

    private async Task ApplyPathRewritingAsync(HttpContext context, ServiceRoute serviceRoute)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var rewrite in serviceRoute.PathRewrites)
        {
            var newPath = Regex.Replace(path, rewrite.Key, rewrite.Value);
            if (newPath != path)
            {
                context.Request.Path = newPath;
                logger.LogDebug("Rewrote path: {OriginalPath} -> {NewPath}", path, newPath);
                break;
            }
        }

        // Handle query string transformation if needed
        await this.TransformQueryStringAsync(context, serviceRoute);
    }

    private async Task TransformQueryStringAsync(HttpContext context, ServiceRoute serviceRoute)
    {
        // Add default query parameters if configured
        if (serviceRoute.QueryParametersToAdd?.Any() == true)
        {
            var queryDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                context.Request.QueryString.Value
            );

            foreach (var param in serviceRoute.QueryParametersToAdd)
            {
                if (!queryDict.ContainsKey(param.Key))
                {
                    queryDict.Add(param.Key, param.Value);
                }
            }

            context.Request.QueryString = new QueryString(
                "?" + Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", queryDict)
            );
        }
    }

    private void AddHeaders(HttpContext context, ServiceRoute serviceRoute)
    {
        foreach (var header in serviceRoute.HeadersToAdd)
        {
            if (!context.Request.Headers.ContainsKey(header.Key))
            {
                context.Request.Headers.Add(header.Key, header.Value);
            }
        }

        // Add standard gateway headers
        context.Request.Headers.Add("X-Forwarded-Proto", context.Request.Scheme);
        context.Request.Headers.Add("X-Forwarded-Host", context.Request.Host.ToString());
        context.Request.Headers.Add("X-Forwarded-For", this.GetClientIpAddress(context));
        context.Request.Headers.Add("X-Request-Id", context.TraceIdentifier);
    }

    private void RemoveHeaders(HttpContext context, ServiceRoute serviceRoute)
    {
        foreach (var header in serviceRoute.HeadersToRemove)
        {
            context.Request.Headers.Remove(header);
        }
    }

    private async Task TransformResponseBodyAsync(HttpContext context)
    {
        if (!this._ShouldTransformResponse(context))
        {
            return;
        }

        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            if (responseBody.Length > 0)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var originalContent = await new StreamReader(responseBody).ReadToEndAsync();

                // Apply response transformations
                var transformedContent = this.TransformResponseContent(originalContent, context);

                // Write transformed content
                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = null;
                await context.Response.WriteAsync(transformedContent);
            }
            else
            {
                context.Response.Body = originalBodyStream;
            }
        }
        catch
        {
            context.Response.Body = originalBodyStream;
            throw;
        }
    }

    private string TransformResponseContent(string content, HttpContext context)
    {
        // Apply JSON transformations if response is JSON
        if (this.IsJsonResponse(context))
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                var transformedJson = this.TransformJsonResponse(jsonElement, context);
                return JsonSerializer.Serialize(
                    transformedJson,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    }
                );
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return original content
                return content;
            }
        }

        return content;
    }

    private JsonElement TransformJsonResponse(JsonElement jsonElement, HttpContext context)
    {
        // Add gateway metadata to JSON response
        var jsonObject =
            JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText())
            ?? new Dictionary<string, object>();

        if (settings.AddMetadataToResponse)
        {
            jsonObject["_gateway"] = new Dictionary<string, object>
            {
                ["service"] = context.Items["ServiceName"]?.ToString() ?? "unknown",
                ["version"] = "1.0.0",
                ["timestamp"] = DateTime.UtcNow,
                ["requestId"] = context.TraceIdentifier,
            };
        }

        return JsonSerializer.SerializeToElement(jsonObject);
    }

    private bool _ShouldTransformResponse(HttpContext context)
    {
        // Only transform successful JSON responses
        return context.Response.StatusCode >= 200
            && context.Response.StatusCode < 300
            && this.IsJsonResponse(context);
    }

    private bool IsJsonResponse(HttpContext context)
    {
        var contentType = context.Response.ContentType?.ToLowerInvariant();
        return contentType?.Contains("application/json") == true
            || contentType?.Contains("application/hal+json") == true;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try to get real IP from headers (for load balancer scenarios)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Transformation settings
/// </summary>
public class TransformationSettings
{
    public bool AddGatewayHeaders { get; set; } = true;
    public bool TransformResponseBody { get; set; } = false;
    public bool AddMetadataToResponse { get; set; } = false;
    public bool EnableTransformationLogging { get; set; } = true;
    public List<string> HeadersToRemove { get; set; } = new();
}

/// <summary>
/// Service route for transformation
/// </summary>
public class ServiceRoute
{
    public string ServiceName { get; set; } = string.Empty;
    public string PathPrefix { get; set; } = string.Empty;
    public Dictionary<string, string> PathRewrites { get; set; } = new();
    public Dictionary<string, string> HeadersToAdd { get; set; } = new();
    public List<string> HeadersToRemove { get; set; } = new();
    public Dictionary<string, string> QueryParametersToAdd { get; set; } = new();
}

/// <summary>
/// Extension methods for registering transformation middleware
/// </summary>
public static class TransformationMiddlewareExtensions
{
    /// <summary>
    /// Adds transformation middleware with default settings
    /// </summary>
    public static IApplicationBuilder UseRequestTransformation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TransformationMiddleware>(new TransformationSettings());
    }

    /// <summary>
    /// Adds transformation middleware with custom settings
    /// </summary>
    public static IApplicationBuilder UseRequestTransformation(
        this IApplicationBuilder builder,
        Action<TransformationSettings> configureSettings
    )
    {
        var settings = new TransformationSettings();
        configureSettings(settings);
        return builder.UseMiddleware<TransformationMiddleware>(settings);
    }
}
