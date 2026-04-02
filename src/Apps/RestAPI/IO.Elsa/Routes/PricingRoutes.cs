using Microsoft.AspNetCore.Mvc;
using IO.Elsa.Core.Pricing;
using IO.Elsa.Models;
using IO.Platform.Common.Core.Routing;

namespace IO.Elsa.Routes;

/// <summary>
/// Static route definitions for Pricing API endpoints
/// </summary>
public static class PricingRoutes
{
    /// <summary>
    /// Maps all pricing-related endpoints for version 1 API
    /// </summary>
    public static IEndpointRouteBuilder MapElsaRoutesV1(this IEndpointRouteBuilder routes)
    {
        // Group pricing routes with common prefix and authorization
        var pricingGroup = routes.CreateApiGroup("pricing", "Pricing", "Pricing API");

        // SPAPI pricing lookup endpoint
        var spapiGroup = pricingGroup.MapGroup("spapi")
            .WithTags("SPAPI")
            .WithDisplayName("SPAPI Pricing");

        spapiGroup.MapPost("lookup", async (
            [FromBody] SpapiPricingRequest request,
            ISpapiPricingService pricingService,
            ILogger logger) =>
        {
            logger.LogInformation("Processing pricing lookup for {StopCount} stops", 
                request.Stops?.Count ?? 0);

            var result = await pricingService.CalculatePriceAsync(request);

            return Results.Ok(new
            {
                request = request,
                pricing = result
            });
        })
        .WithName("SpapiPriceLookup")
        .Produces(200)
        .Produces(400);

        // Get rate card endpoint - removed since we don't have rate card repository
        // This would be implemented if needed in the future

        // Calculate distance endpoint - removed since we don't have distance calculation service
        // This would be implemented if needed in the future

        return routes;
    }

    /// <summary>
    /// Maps all pricing-related endpoints (legacy method for backward compatibility)
    /// </summary>
    public static IEndpointRouteBuilder MapPricingRoutes(this IEndpointRouteBuilder routes)
    {
        return routes.MapElsaRoutesV1();
    }
}
