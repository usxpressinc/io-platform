using Microsoft.AspNetCore.Mvc;
using IO.Proxy.Core.Health;
using IO.Proxy.Core.Monitoring;
using IO.Proxy.Infrastructure.Http;
using IO.Platform.Common.Core.Routing;

namespace IO.Proxy.Routes;

/// <summary>
/// Static route definitions for Gateway API endpoints
/// </summary>
public static class GatewayRoutes
{
    /// <summary>
    /// Maps all gateway management endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapGatewayRoutes(this IEndpointRouteBuilder routes)
    {
        // Group gateway routes (no authorization for management endpoints)
        var gatewayGroup = routes.MapGroup("/api/gateway")
            .WithTags("Gateway")
            .WithDisplayName("Gateway Management API");

        // Health check endpoint
        gatewayGroup.MapGet("health", async (
            IHealthAggregationService healthService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var health = await healthService.GetOverallHealthAsync();
                
                return health.Status switch
                {
                    HealthStatus.Healthy => Results.Ok(new { status = "healthy", health }),
                    HealthStatus.Degraded => Results.Ok(new { status = "degraded", health }),
                    HealthStatus.Unhealthy => Results.StatusCode(503, new { status = "unhealthy", health }),
                    _ => Results.StatusCode(500, new { status = "unknown", health })
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting gateway health");
                return Results.StatusCode(500, new { error = "Health check failed" });
            }
        })
        .WithName("GatewayHealth")
        .Produces(200)
        .Produces(503)
        .Produces(500);

        // Readiness check endpoint
        gatewayGroup.MapGet("ready", async (
            IHealthAggregationService healthService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var health = await healthService.GetOverallHealthAsync();
                
                if (health.Status == HealthStatus.Unhealthy)
                {
                    return Results.StatusCode(503, ApiResponsePatterns.Error("Gateway is unhealthy", new { 
                        status = "not_ready", 
                        reason = "Gateway is unhealthy",
                        unhealthyServices = health.UnhealthyServices
                    }));
                }

                return Results.Ok(ApiResponsePatterns.Success(new { 
                    status = "ready",
                    version = "1.0.0",
                    uptime = health.Uptime,
                    services = health.Services.Count
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking gateway readiness");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Readiness check failed"));
            }
        })
        .WithName("GatewayReadiness")
        .Produces(200)
        .Produces(503);

        // Metrics endpoint
        gatewayGroup.MapGet("metrics", (
            IGatewayMonitoringService monitoringService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var metrics = monitoringService.GetMetrics();
                return Results.Ok(ApiResponsePatterns.Success(metrics));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting gateway metrics");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Metrics retrieval failed"));
            }
        })
        .WithName("GatewayMetrics")
        .Produces(200)
        .Produces(500);

        // Service registry endpoint
        gatewayGroup.MapGet("services", async (
            IHealthAggregationService healthService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var health = await healthService.GetOverallHealthAsync();
                
                var services = health.Services.Select(s => new
                {
                    name = s.ServiceName,
                    status = s.Status.ToString().ToLowerInvariant(),
                    lastCheck = s.LastCheck,
                    responseTime = s.ResponseTime,
                    error = s.ErrorMessage,
                    dependencies = s.Dependencies?.Select(d => new
                    {
                        name = d.Name,
                        status = d.Status.ToString().ToLowerInvariant(),
                        responseTime = d.ResponseTime
                    })
                });

                return Results.Ok(ApiResponsePatterns.Success(new { 
                    services,
                    total = services.Count(),
                    healthy = services.Count(s => s.status == "healthy"),
                    degraded = services.Count(s => s.status == "degraded"),
                    unhealthy = services.Count(s => s.status == "unhealthy")
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service registry");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Service registry retrieval failed"));
            }
        })
        .WithName("ServiceRegistry")
        .Produces(200)
        .Produces(500);

        // Gateway info endpoint
        gatewayGroup.MapGet("info", (
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var info = new
                {
                    name = "IO.Proxy Gateway",
                    version = "1.0.0",
                    description = "API Gateway for IO Platform microservices",
                    startedAt = DateTime.UtcNow.AddHours(-1), // This would come from actual startup time
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    frameworks = new[]
                    {
                        "ASP.NET Core 10.0",
                        "USXpress.Monitoring",
                        "USXpress.Configuration.Mongo",
                        "USXpress.Kafka"
                    },
                    capabilities = new[]
                    {
                        "Request routing",
                        "Load balancing",
                        "Authentication forwarding",
                        "Request/response transformation",
                        "Health check aggregation",
                        "Metrics collection"
                    }
                };

                return Results.Ok(ApiResponsePatterns.Success(info));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting gateway info");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Info retrieval failed"));
            }
        })
        .WithName("GatewayInfo")
        .Produces(200)
        .Produces(500);

        return routes;
    }

    /// <summary>
    /// Maps service-specific endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapServiceRoutes(this IEndpointRouteBuilder routes)
    {
        var serviceGroup = routes.MapGroup("/api/services")
            .WithTags("Services")
            .WithDisplayName("Service Management API");

        // Service-specific health check
        serviceGroup.MapGet("{serviceName}/health", async (
            string serviceName,
            IHealthAggregationService healthService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var health = await healthService.GetServiceHealthAsync(serviceName);
                
                return health.Status switch
                {
                    HealthStatus.Healthy => Results.Ok(ApiResponsePatterns.Success(new { status = "healthy", service = health })),
                    HealthStatus.Degraded => Results.Ok(ApiResponsePatterns.Success(new { status = "degraded", service = health })),
                    HealthStatus.Unhealthy => Results.StatusCode(503, ApiResponsePatterns.Error("Service is unhealthy", new { status = "unhealthy", service = health })),
                    HealthStatus.Unknown => Results.StatusCode(503, ApiResponsePatterns.Error("Service status unknown", new { status = "unknown", service = health })),
                    _ => Results.StatusCode(500, ApiResponsePatterns.Error("Error checking service health", new { status = "error", service = health }))
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service health for {ServiceName}", serviceName);
                return Results.StatusCode(500, ApiResponsePatterns.Error($"Health check failed for {serviceName}"));
            }
        })
        .WithName("ServiceHealth")
        .Produces(200)
        .Produces(503);

        // Service-specific metrics
        serviceGroup.MapGet("{serviceName}/metrics", (
            string serviceName,
            IGatewayMonitoringService monitoringService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var metrics = monitoringService.GetServiceMetrics(serviceName);
                return Results.Ok(ApiResponsePatterns.Success(metrics));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service metrics for {ServiceName}", serviceName);
                return Results.StatusCode(500, ApiResponsePatterns.Error($"Metrics retrieval failed for {serviceName}"));
            }
        })
        .WithName("ServiceMetrics")
        .Produces(200)
        .Produces(500);

        // Refresh service health
        serviceGroup.MapPost("{serviceName}/health/refresh", async (
            string serviceName,
            IHealthAggregationService healthService,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                await healthService.RefreshServiceHealthAsync(serviceName);
                var health = await healthService.GetServiceHealthAsync(serviceName);
                
                return Results.Ok(ApiResponsePatterns.Success(new { 
                    status = "refreshed",
                    service = health,
                    timestamp = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing service health for {ServiceName}", serviceName);
                return Results.StatusCode(500, ApiResponsePatterns.Error($"Health refresh failed for {serviceName}"));
            }
        })
        .WithName("RefreshServiceHealth")
        .Produces(200)
        .Produces(500);

        return routes;
    }

    /// <summary>
    /// Maps proxy endpoints for downstream services
    /// </summary>
    public static IEndpointRouteBuilder MapProxyRoutes(this IEndpointRouteBuilder routes)
    {
        var proxyGroup = routes.CreateApiGroup("", "Proxy", "Service Proxy API");

        // Email proxy
        proxyGroup.MapPost("email/send", async (
            [FromBody] object request,
            IHttpClientFactory httpClientFactory,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-common");
                var response = await client.PostAsJsonAsync("/api/email/send", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(ApiResponsePatterns.Success(result));
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode, ApiResponsePatterns.Error(error));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying email send request");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Internal server error"));
            }
        })
        .WithName("ProxyEmailSend")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Carrier validation proxy
        proxyGroup.MapPost("carriers/valid", async (
            [FromBody] object request,
            IHttpClientFactory httpClientFactory,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-cass");
                var response = await client.PostAsJsonAsync("/api/carriers/valid", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(ApiResponsePatterns.Success(result));
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode, ApiResponsePatterns.Error(error));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying carrier validation request");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Internal server error"));
            }
        })
        .WithName("ProxyCarrierValid")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Pricing lookup proxy
        proxyGroup.MapPost("price/lookup", async (
            [FromBody] object request,
            IHttpClientFactory httpClientFactory,
            ILogger<GatewayRoutes> logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-elsa");
                var response = await client.PostAsJsonAsync("/api/pricing/spapi/lookup", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(ApiResponsePatterns.Success(result));
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode, ApiResponsePatterns.Error(error));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying price lookup request");
                return Results.StatusCode(500, ApiResponsePatterns.Error("Internal server error"));
            }
        })
        .WithName("ProxyPriceLookup")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        return routes;
    }
}
