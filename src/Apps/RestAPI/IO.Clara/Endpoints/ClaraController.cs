using IO.Clara.Infrastructure.Highway;
using IO.Clara.Models;
using Microsoft.AspNetCore.Mvc;

namespace IO.Clara.Endpoints;

/// <summary>
/// Clara endpoints for carrier validation
/// </summary>
[ApiController]
[Route("api/clara")]
public class ClaraController(IHighwayApiClient highwayApiClient, ILogger<ClaraController> logger)
    : ControllerBase
{
    /// <summary>
    /// Check whether a carrier is valid using Highway API
    /// </summary>
    [HttpPost("carriers/valid")]
    public async Task<IActionResult> GetCarrierValidity([FromBody] CarrierValidityRequest request)
    {
        try
        {
            logger.LogInformation(
                "Checking carrier validity for DOT: {DotNumber}, MC: {McNumber}",
                request.DotNumber,
                request.McNumber
            );

            // Get Highway data - simplified like Python
            var highwayData = await highwayApiClient.GetHighwayDataAsync(
                request.DotNumber ?? string.Empty,
                request.McNumber ?? string.Empty
            );

            if (highwayData == null)
            {
                return this.Ok(
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

            // Simple connection checks (matching Python logic)
            if (highwayData.Connection?.Status == "do_not_dispatch")
            {
                return this.Ok(
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
            }

            if (highwayData.Connection?.Status == "needs_to_onboard")
            {
                return this.Ok(
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
                return this.Ok(
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
            return this.Ok(
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
            return this.Ok(
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
