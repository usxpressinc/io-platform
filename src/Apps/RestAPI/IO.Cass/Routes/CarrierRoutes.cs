using Microsoft.AspNetCore.Mvc;
using IO.Cass.Core.Carriers;
using IO.Cass.Models;
using IO.Platform.Common.Core.Routing;

namespace IO.Cass.Routes;

/// <summary>
/// Static route definitions for Carrier Validation API endpoints
/// </summary>
public static class CarrierRoutes
{
    /// <summary>
    /// Maps all carrier-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapCarrierRoutes(this IEndpointRouteBuilder routes)
    {
        // Group carrier routes with common prefix and authorization
        var carrierGroup = routes.CreateApiGroup("carriers", "Carriers", "Carrier Validation API");

        // Vet a carrier by DOT number
        carrierGroup.MapPost("{dotNumber}/vet", async (
            string dotNumber,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return Results.BadRequest(new { error = "DOT number is required" });
            }

            logger.LogInformation("User starting carrier vetting for DOT number {DotNumber}", dotNumber);

            var result = await vettingService.VetCarrierAsync(dotNumber);

            return result.Success 
                ? Results.Ok(new
                {
                    carrier = result.Carrier,
                    vettingStatus = result.VettingStatus,
                    vettingScore = result.VettingScore,
                    vettingDate = result.VettingDate
                })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("VetCarrier")
        .Produces(200)
        .Produces(400);

        // Re-vet a carrier by DOT number
        carrierGroup.MapPost("{dotNumber}/re-vet", async (
            string dotNumber,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return Results.BadRequest(new { error = "DOT number is required" });
            }

            logger.LogInformation("User starting carrier re-vetting for DOT number {DotNumber}", dotNumber);

            var result = await vettingService.ReVetCarrierAsync(dotNumber);

            return result.Success 
                ? Results.Ok(ApiResponsePatterns.Success(new
                {
                    carrier = result.Carrier,
                    vettingStatus = result.VettingStatus,
                    vettingScore = result.VettingScore,
                    vettingDate = result.VettingDate,
                    previousVettingStatus = result.PreviousVettingStatus,
                    previousVettingScore = result.PreviousVettingScore,
                    previousVettingDate = result.PreviousVettingDate
                }))
                : Results.BadRequest(ApiResponsePatterns.Error(result.ErrorMessage));
        })
        .WithName("ReVetCarrier")
        .Produces(200)
        .Produces(400);

        // Get carrier by DOT number
        carrierGroup.MapGet("{dotNumber}", async (
            string dotNumber,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return Results.BadRequest(new { error = "DOT number is required" });
            }

            logger.LogInformation("Getting carrier for DOT number {DotNumber}", dotNumber);

            var carrier = await vettingService.GetCarrierByDotNumberAsync(dotNumber);

            return carrier != null 
                ? Results.Ok(carrier)
                : Results.NotFound(new { error = "Carrier not found" });
        })
        .WithName("GetCarrier")
        .Produces(200)
        .Produces(404);

        // Search carriers
        carrierGroup.MapPost("search", async (
            [FromBody] CarrierSearchRequest request,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Search request is required" });
            }

            logger.LogInformation("Searching carriers with criteria: {Criteria}", request);

            var carriers = await vettingService.SearchCarriersAsync(request);

            return Results.Ok(new { 
                carriers,
                count = carriers.Count
            });
        })
        .WithName("SearchCarriers")
        .Produces(200)
        .Produces(400);

        // Get carriers by vetting status
        carrierGroup.MapGet("by-vetting-status/{status}", async (
            VettingStatus status,
            [FromQuery] int limit = 100,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            logger.LogInformation("Getting carriers with vetting status {Status}", status);

            var carriers = await vettingService.GetCarriersByVettingStatusAsync(status, limit);

            return Results.Ok(new { 
                carriers,
                count = carriers.Count,
                status
            });
        })
        .WithName("GetCarriersByVettingStatus")
        .Produces(200);

        // Update carrier vetting status
        carrierGroup.MapPut("{dotNumber}/vetting-status", async (
            string dotNumber,
            [FromBody] UpdateVettingStatusRequest request,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return Results.BadRequest(new { error = "DOT number is required" });
            }

            if (request == null)
            {
                return Results.BadRequest(new { error = "Vetting status request is required" });
            }

            logger.LogInformation("Updating vetting status for DOT number {DotNumber} to {Status} with score {Score}", 
                dotNumber, request.Status, request.Score);

            var success = await vettingService.UpdateCarrierVettingStatusAsync(dotNumber, request.Status, request.Score);

            return success 
                ? Results.Ok(new { message = "Vetting status updated successfully" })
                : Results.StatusCode(500, new { error = "Failed to update vetting status" });
        })
        .WithName("UpdateVettingStatus")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Check whether a carrier is valid using Highway API (matches Python functionality)
        carrierGroup.MapPost("valid", async (
            [FromBody] CarrierValidityRequest request,
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            logger.LogInformation("Checking carrier validity for DOT: {DotNumber}, MC: {McNumber}", 
                request.DotNumber, request.McNumber);

            try
            {
                var result = await vettingService.VetCarrierAsync(request.DotNumber ?? string.Empty);

                var response = new CarrierValidityResponse
                {
                    IsValid = result.Success ? "true" : "false",
                    StatusCode = result.Success ? 200 : 400,
                    Contacts = result.Carrier != null ? new List<CarrierContact>
                    {
                        new CarrierContact
                        {
                            Name = result.Carrier.LegalName,
                            EmailAddresses = new List<string> { result.Carrier.Email },
                            Phones = new List<string> { result.Carrier.Phone },
                            IsType = "primary"
                        }
                    } : new List<CarrierContact>()
                };

                if (!result.Success)
                {
                    response.Errors.Add(new CarrierValidityError
                    {
                        Code = "CARRIER_INVALID",
                        Description = result.ErrorMessage ?? "Carrier validation failed",
                        Classification = "validation"
                    });
                    response.FailedBy.Add("vetting_check");
                }

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking carrier validity");
                return Results.Ok(new CarrierValidityResponse
                {
                    IsValid = "false",
                    StatusCode = 500,
                    Errors = new List<CarrierValidityError>
                    {
                        new CarrierValidityError
                        {
                            Code = "SYSTEM_ERROR",
                            Description = "Internal system error occurred",
                            Classification = "system"
                        }
                    },
                    FailedBy = new List<string> { "system_check" }
                });
            }
        })
        .WithName("GetCarrierValidity")
        .Produces(200);

        // Get carrier statistics
        carrierGroup.MapGet("statistics", async (
            ICarrierVettingService vettingService,
            ILogger<CarrierRoutes> logger) =>
        {
            logger.LogInformation("Getting carrier statistics");

            try
            {
                // This would typically use a repository method to get aggregated statistics
                var statistics = new
                {
                    totalCarriers = 0,
                    approvedCarriers = 0,
                    rejectedCarriers = 0,
                    requiresReviewCarriers = 0,
                    notVettedCarriers = 0,
                    averageVettingScore = 0.0,
                    lastUpdated = DateTime.UtcNow
                };

                return Results.Ok(statistics);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting carrier statistics");
                return Results.StatusCode(500, new { error = "Internal server error" });
            }
        })
        .WithName("GetCarrierStatistics")
        .Produces(200)
        .Produces(500);

        return routes;
    }
}

/// <summary>
/// Update vetting status request
/// </summary>
public class UpdateVettingStatusRequest
{
    public VettingStatus Status { get; set; }
    public double Score { get; set; }
}
