using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IO.Platform.Common.Core.Routing;

/// <summary>
/// Base extensions for organizing minimal API routes
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Adds standard health endpoints to a route group
    /// </summary>
    public static RouteGroupBuilder AddStandardHealthEndpoints(this RouteGroupBuilder group, string serviceName)
    {
        group.MapGet("/health", () => Results.Ok(new { 
            service = serviceName,
            status = "healthy", 
            timestamp = DateTime.UtcNow 
        }))
        .WithName($"{serviceName}Health")
        .ExcludeFromDescription();

        group.MapGet("/ready", () => Results.Ok(new { 
            service = serviceName,
            status = "ready", 
            timestamp = DateTime.UtcNow 
        }))
        .WithName($"{serviceName}Readiness")
        .ExcludeFromDescription();

        return group;
    }

    /// <summary>
    /// Adds standard API info endpoint
    /// </summary>
    public static IEndpointRouteBuilder AddApiInfo(this IEndpointRouteBuilder routes, string serviceName, string description)
    {
        routes.MapGet("/", () => Results.Json(new 
        { 
            service = serviceName,
            version = "1.0.0",
            description = description,
            timestamp = DateTime.UtcNow,
            documentation = "/swagger"
        }))
        .WithName("ApiInfo")
        .ExcludeFromDescription();

        return routes;
    }

    /// <summary>
    /// Creates a standard API group with authorization and tags
    /// </summary>
    public static RouteGroupBuilder CreateApiGroup(this IEndpointRouteBuilder routes, string prefix, string tag, string displayName)
    {
        return routes.MapGroup($"/api/{prefix}")
            .RequireAuthorization()
            .WithTags(tag)
            .WithDisplayName(displayName);
    }

    /// <summary>
    /// Adds error handling wrapper for route handlers
    /// </summary>
    public static Delegate WithErrorHandling(this Delegate handler, ILogger<object> logger)
    {
        return async (object[] args) =>
        {
            try
            {
                return (IResult)handler.DynamicInvoke(args)!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in route handler");
                return Results.StatusCode(500);
            }
        };
    }

    /// <summary>
    /// Adds validation wrapper for requests
    /// </summary>
    public static Delegate WithValidation<T>(this Delegate handler, ILogger<object> logger) where T : class
    {
        return async (object[] args) =>
        {
            try
            {
                var request = args.OfType<T>().FirstOrDefault();
                if (request == null)
                {
                    return Results.BadRequest(new { error = "Invalid request" });
                }

                // Add validation logic here if needed
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    return Results.BadRequest(new { 
                        error = "Validation failed",
                        details = validationResults.Select(v => new
                        {
                            field = v.MemberNames.FirstOrDefault(),
                            message = v.ErrorMessage
                        })
                    });
                }

                return (IResult)handler.DynamicInvoke(args)!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in route handler");
                return Results.StatusCode(500);
            }
        };
    }
}

/// <summary>
/// Standard response patterns for APIs
/// </summary>
public static class ApiResponsePatterns
{
    /// <summary>
    /// Creates a standard success response
    /// </summary>
    public static object Success(object? data = null, string? message = null)
    {
        return new
        {
            success = true,
            data = data,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a standard error response
    /// </summary>
    public static object Error(string message, string? code = null, object? details = null)
    {
        return new
        {
            success = false,
            error = message,
            code = code,
            details = details,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static object ValidationError(IEnumerable<string> validationErrors)
    {
        return new
        {
            success = false,
            error = "Validation failed",
            errors = validationErrors.Select(e => new { message = e }),
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a paginated response
    /// </summary>
    public static object Paginated<T>(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        return new
        {
            success = true,
            data = items,
            pagination = new
            {
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                hasNext = page * pageSize < totalCount,
                hasPrevious = page > 1
            },
            timestamp = DateTime.UtcNow
        };
    }
}
