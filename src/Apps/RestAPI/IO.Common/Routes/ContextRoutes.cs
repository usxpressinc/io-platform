using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IO.Common.Core;
using IO.Common.Models;
using IO.Platform.Common.Core.Routing;

namespace IO.Common.Routes;

/// <summary>
/// Static route definitions for Context API endpoints
/// </summary>
public static class ContextRoutes
{
    /// <summary>
    /// Maps all context-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapContextRoutes(this IEndpointRouteBuilder routes)
    {
        // Group context routes with common prefix and authorization
        var contextGroup = routes.CreateApiGroup("context", "Context", "Context API");

        // Get user context endpoint
        contextGroup.MapGet("/", async (
            [FromQuery] string? id = null,
            [FromQuery] string? number = null,
            IContextService contextService,
            ILogger<ContextRoutes> logger) =>
        {
            logger.LogInformation("Getting user context for ID: {Id}, Number: {Number}", id, number);

            var context = await contextService.GetContextAsync(id, number);

            return Results.Ok(context);
        })
        .WithName("GetUserContext")
        .Produces(200);

        // Add user context endpoint
        contextGroup.MapPost("/", async (
            [FromBody] ContextRequest request,
            IContextService contextService,
            ILogger<ContextRoutes> logger) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Context request is required" });
            }

            logger.LogInformation("Adding user context for ID: {Id}, Number: {Number}", request.Id, request.Number);

            var success = await contextService.AddContextAsync(request);

            return success 
                ? Results.Ok(new { message = "Context added successfully" })
                : Results.StatusCode(500, new { error = "Failed to add context" });
        })
        .WithName("AddUserContext")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        return routes;
    }

    /// <summary>
    /// Maps Genesys-specific endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapGenesysRoutes(this IEndpointRouteBuilder routes)
    {
        // Group genesys routes
        var genesysGroup = routes.CreateApiGroup("genesys", "Genesys", "Genesys API");

        // Get Genesys driver context endpoint
        genesysGroup.MapGet("driver", async (
            [FromQuery] string? id = null,
            [FromQuery] string? number = null,
            IContextService contextService,
            ILogger<ContextRoutes> logger) =>
        {
            logger.LogInformation("Getting Genesys driver context for ID: {Id}, Number: {Number}", id, number);

            var context = await contextService.GetGenesysDriverContextAsync(id, number);

            return Results.Ok(context);
        })
        .WithName("GetGenesysDriverContext")
        .Produces(200);

        return routes;
    }
}
