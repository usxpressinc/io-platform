using Microsoft.AspNetCore.Mvc;
using IO.Clara.Infrastructure.Highway;
using IO.Clara.Models;
using IO.Platform.Common.Core.Routing;

namespace IO.Clara.Routes;

/// <summary>
/// Static route definitions for Clara API endpoints
/// </summary>
public static class ClaraRoutes
{
    /// <summary>
    /// Maps all Clara-related endpoints for version 1 API
    /// </summary>
    public static IEndpointRouteBuilder MapClaraRoutesV1(this IEndpointRouteBuilder routes)
    {
        // Group Clara routes with common prefix and authorization
        var claraGroup = routes.CreateApiGroup("/api/clara", "Clara", "Clara API");

        // Carrier validation endpoints
        var carrierGroup = claraGroup.MapGroup("carriers")
            .WithTags("Carriers")
            .WithDisplayName("Carrier Validation");

        carrierGroup.MapPost("valid", async (
            [FromBody] CarrierValidityRequest request,
            IHighwayApiClient highwayApiClient,
            ILogger logger) =>
        {
            logger.LogInformation(
                "Checking carrier validity for DOT: {DotNumber}, MC: {McNumber}",
                request.DotNumber,
                request.McNumber
            );

            try
            {
                // Get Highway data - simplified like Python
                var highwayData = await highwayApiClient.GetHighwayDataAsync(
                    request.DotNumber ?? string.Empty,
                    request.McNumber ?? string.Empty
                );

                if (highwayData == null)
                {
                    return Results.Ok(
                        new CarrierValidityResponse
                        {
                            IsValid = "false",
                            StatusCode = 400,
                            Errors =
                            [
                                new CarrierValidityError
                                {
                                    Code = "HIGHWAY_CONNECTION_FAILED",
                                    Description = "Unable to connect to Highway API",
                                    Classification = "system",
                                },
                            ],
                            FailedBy = ["connection=null"],
                        }
                    );
                }

                // Set contacts from Highway data (like Python's set_carrier_contacts)
                var contacts = ExtractHighwayContacts(highwayData);

                switch (highwayData.Connection?.Status)
                {
                    // Simple connection checks (matching Python logic)
                    case "do_not_dispatch":
                        return Results.Ok(
                            new CarrierValidityResponse
                            {
                                IsValid = "false",
                                StatusCode = 200,
                                Errors =
                                [
                                    new CarrierValidityError
                                    {
                                        Code = "DO_NOT_USE",
                                        Description = "Carrier is marked as do not dispatch",
                                        Classification = "compliance",
                                    },
                                ],
                                FailedBy = ["connection.status=do_not_dispatch"],
                                Contacts = contacts,
                            }
                        );
                    case "needs_to_onboard":
                        return Results.Ok(
                            new CarrierValidityResponse
                            {
                                IsValid = "false",
                                StatusCode = 200,
                                Errors =
                                [
                                    new CarrierValidityError
                                    {
                                        Code = "HIGHWAY_CONNECT",
                                        Description = "Carrier needs to onboard with Highway",
                                        Classification = "onboarding",
                                    },
                                ],
                                FailedBy = ["connection.status=needs_to_onboard"],
                                Contacts = contacts,
                            }
                        );
                }

                // Check if connection is monitored
                if (highwayData.Connection?.IsMonitored != true)
                {
                    return Results.Ok(
                        new CarrierValidityResponse
                        {
                            IsValid = "false",
                            StatusCode = 200,
                            Errors =
                            [
                                new CarrierValidityError
                                {
                                    Code = "NOT_MONITORED",
                                    Description = "Carrier connection is not monitored",
                                    Classification = "monitoring",
                                },
                            ],
                            FailedBy = ["connection.is_monitored=false"],
                            Contacts = contacts,
                        }
                    );
                }

                // If all checks pass, carrier is valid
                return Results.Ok(
                    new CarrierValidityResponse
                    {
                        IsValid = "true",
                        StatusCode = 200,
                        Contacts = contacts,
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking carrier validity");
                return Results.Ok(
                    new CarrierValidityResponse
                    {
                        IsValid = "false",
                        StatusCode = 500,
                        Errors =
                        [
                            new CarrierValidityError
                            {
                                Code = "SYSTEM_ERROR",
                                Description = "Internal system error occurred",
                                Classification = "system",
                            },
                        ],
                        FailedBy = ["system_check"],
                    }
                );
            }
        })
        .WithName("CarrierValidityCheck")
        .Produces<CarrierValidityResponse>(200)
        .Produces(400);

        // Pricing endpoints
        var pricingGroup = claraGroup.MapGroup("pricing")
            .WithTags("Pricing")
            .WithDisplayName("Pricing");

        pricingGroup.MapPost("calculate", async (
            [FromBody] PriceRequest request,
            ILogger logger) =>
        {
            logger.LogInformation("Processing pricing calculation for Clara");

            // TODO: Implement pricing calculation logic
            // This is a placeholder - you would inject a pricing service here
            
            return Results.Ok(new PriceResponse
            {
                Amount = null,
                Currency = "USD",
                QuoteId = "placeholder",
                Status = "pending_implementation"
            });
        })
        .WithName("ClaraPriceCalculate")
        .Produces<PriceResponse>(200)
        .Produces(400);

        return routes;
    }

    /// <summary>
    /// Maps all Clara-related endpoints (legacy method for backward compatibility)
    /// </summary>
    public static IEndpointRouteBuilder MapClaraRoutes(this IEndpointRouteBuilder routes)
    {
        return routes.MapClaraRoutesV1();
    }

    private static List<CarrierContact> ExtractHighwayContacts(HighwayCarrierData highwayData)
    {
        var contacts = new List<CarrierContact>();

        if (highwayData.ContactInformation?.DispatchContact != null)
        {
            var dc = highwayData.ContactInformation.DispatchContact;
            contacts.Add(
                new CarrierContact
                {
                    Name = dc.Name ?? "dispatch",
                    IsType = "dispatch",
                    Phones = !string.IsNullOrEmpty(dc.Phone) ? [dc.Phone] : [],
                    EmailAddresses = !string.IsNullOrEmpty(dc.EmailAddress)
                        ? [dc.EmailAddress]
                        : [],
                }
            );
        }

        contacts.AddRange(
            (highwayData.ContactInformation?.LineItemContacts ?? []).Select(
                contact => new CarrierContact
                {
                    Name = contact.Name ?? contact.IsType ?? "unknown",
                    IsType = contact.IsType ?? "unknown",
                    Phones = !string.IsNullOrEmpty(contact.Phone) ? [contact.Phone] : [],
                    EmailAddresses = !string.IsNullOrEmpty(contact.EmailAddress)
                        ? [contact.EmailAddress]
                        : [],
                }
            )
        );

        return contacts;
    }
}
