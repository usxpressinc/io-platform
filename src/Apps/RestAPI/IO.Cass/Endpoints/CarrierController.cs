using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Cass.Core.Carriers;
using IO.Cass.Infrastructure.Data;
using IO.Cass.Models;

namespace IO.Cass.Endpoints;

/// <summary>
/// Carrier endpoints for IO.Cass service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarrierController : ControllerBase
{
    private readonly ICarrierVettingService _carrierVettingService;
    private readonly ILogger<CarrierController> _logger;

    public CarrierController(
        ICarrierVettingService carrierVettingService,
        ILogger<CarrierController> logger)
    {
        _carrierVettingService = carrierVettingService;
        _logger = logger;
    }

    /// <summary>
    /// Vet a carrier by DOT number
    /// </summary>
    [HttpPost("{dotNumber}/vet")]
    public async Task<IActionResult> VetCarrier(string dotNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return BadRequest(new { error = "DOT number is required" });
            }

            _logger.LogInformation("User starting carrier vetting for DOT number {DotNumber}", dotNumber);

            var result = await _carrierVettingService.VetCarrierAsync(dotNumber);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    carrier = result.Carrier,
                    vettingStatus = result.VettingStatus,
                    vettingScore = result.VettingScore,
                    vettingDate = result.VettingDate
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vetting carrier {DotNumber}", dotNumber);
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Re-vet a carrier by DOT number
    /// </summary>
    [HttpPost("{dotNumber}/re-vet")]
    public async Task<IActionResult> ReVetCarrier(string dotNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return BadRequest(new { error = "DOT number is required" });
            }

            _logger.LogInformation("User starting carrier re-vetting for DOT number {DotNumber}", dotNumber);

            var result = await _carrierVettingService.ReVetCarrierAsync(dotNumber);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    carrier = result.Carrier,
                    vettingStatus = result.VettingStatus,
                    vettingScore = result.VettingScore,
                    vettingDate = result.VettingDate,
                    previousVettingStatus = result.PreviousVettingStatus,
                    previousVettingScore = result.PreviousVettingScore,
                    previousVettingDate = result.PreviousVettingDate
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-vetting carrier {DotNumber}", dotNumber);
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get carrier by DOT number
    /// </summary>
    [HttpGet("{dotNumber}")]
    public async Task<IActionResult> GetCarrier(string dotNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return BadRequest(new { error = "DOT number is required" });
            }

            _logger.LogInformation("Getting carrier for DOT number {DotNumber}", dotNumber);

            var carrier = await _carrierVettingService.GetCarrierByDotNumberAsync(dotNumber);

            if (carrier == null)
            {
                return NotFound(new { error = "Carrier not found" });
            }

            return Ok(carrier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier {DotNumber}", dotNumber);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Search carriers
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchCarriers([FromBody] CarrierSearchRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Search request is required" });
            }

            _logger.LogInformation("Searching carriers with criteria: {Criteria}", request);

            var carriers = await _carrierVettingService.SearchCarriersAsync(request);

            return Ok(new { 
                carriers,
                count = carriers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching carriers");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get carriers by vetting status
    /// </summary>
    [HttpGet("by-vetting-status/{status}")]
    public async Task<IActionResult> GetCarriersByVettingStatus(VettingStatus status, [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting carriers with vetting status {Status}", status);

            var carriers = await _carrierVettingService.GetCarriersByVettingStatusAsync(status, limit);

            return Ok(new { 
                carriers,
                count = carriers.Count,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carriers by vetting status {Status}", status);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Update carrier vetting status
    /// </summary>
    [HttpPut("{dotNumber}/vetting-status")]
    public async Task<IActionResult> UpdateVettingStatus(string dotNumber, [FromBody] UpdateVettingStatusRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(dotNumber))
            {
                return BadRequest(new { error = "DOT number is required" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Vetting status request is required" });
            }

            _logger.LogInformation("Updating vetting status for DOT number {DotNumber} to {Status} with score {Score}", 
                dotNumber, request.Status, request.Score);

            var success = await _carrierVettingService.UpdateCarrierVettingStatusAsync(dotNumber, request.Status, request.Score);

            if (success)
            {
                return Ok(new { message = "Vetting status updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update vetting status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vetting status for DOT number {DotNumber}", dotNumber);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Check whether a carrier is valid using Highway API (matches Python functionality)
    /// </summary>
    [HttpPost("valid")]
    public async Task<IActionResult> GetCarrierValidity([FromBody] CarrierValidityRequest request)
    {
        try
        {
            _logger.LogInformation("Checking carrier validity for DOT: {DotNumber}, MC: {McNumber}", 
                request.DotNumber, request.McNumber);

            // Use the existing vetting service to check validity
            var result = await _carrierVettingService.VetCarrierAsync(request.DotNumber ?? string.Empty);

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

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking carrier validity");
            return Ok(new CarrierValidityResponse
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
    }

    /// <summary>
    /// Get carrier statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetCarrierStatistics()
    {
        try
        {
            _logger.LogInformation("Getting carrier statistics");

            // This would typically use a repository method to get aggregated statistics
            // For now, return placeholder data
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

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier statistics");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
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
