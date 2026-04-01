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
    /// Maps all pricing-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapPricingRoutes(this IEndpointRouteBuilder routes)
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
            ILogger<PricingRoutes> logger) =>
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

        // Get rate card endpoint
        pricingGroup.MapGet("ratecard", async (
            [FromQuery] string origin,
            [FromQuery] string destination,
            [FromQuery] DateTime? effectiveDate = null,
            IPricingService pricingService,
            ILogger<PricingRoutes> logger) =>
        {
            logger.LogInformation("Getting rate card from {Origin} to {Destination}", origin, destination);

            var rateCard = await pricingService.GetRateCardAsync(origin, destination, effectiveDate);

            return Results.Ok(new
            {
                origin,
                destination,
                effective_date = effectiveDate ?? DateTime.UtcNow,
                rate_card = rateCard
            });
        })
        .WithName("GetRateCard")
        .Produces(200)
        .Produces(404);

        // Calculate distance endpoint
        pricingGroup.MapPost("distance", async (
            [FromBody] DistanceCalculationRequest request,
            IPricingService pricingService,
            ILogger<PricingRoutes> logger) =>
        {
            logger.LogInformation("Calculating distance for {StopCount} stops", 
                request.Stops?.Count ?? 0);

            var distance = await pricingService.CalculateDistanceAsync(request);

            return Results.Ok(new
            {
                request = request,
                distance = distance
            });
        })
        .WithName("CalculateDistance")
        .Produces(200)
        .Produces(400);

        return routes;
    }
}
