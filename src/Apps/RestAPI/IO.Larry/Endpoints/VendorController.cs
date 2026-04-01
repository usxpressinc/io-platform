using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Larry.Core.Vendors;
using IO.Larry.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Larry.Endpoints;

/// <summary>
/// Vendor endpoints for IO.Larry service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendorController : ControllerBase
{
    private readonly IVendorBusinessService _vendorService;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorBusinessService vendorService,
        ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new vendor
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVendor([FromBody] Vendor vendor)
    {
        try
        {
            if (vendor == null)
            {
                return BadRequest(new { error = "Vendor data is required" });
            }

            _logger.LogInformation("Creating vendor {VendorCode}", vendor.VendorCode);

            var createdVendor = await _vendorService.CreateVendorAsync(vendor);

            return CreatedAtAction(
                nameof(GetVendor),
                new { id = createdVendor.Id },
                createdVendor);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating vendor");
            return BadRequest(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict creating vendor");
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendor by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVendor(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Vendor ID is required" });
            }

            _logger.LogInformation("Getting vendor {Id}", id);

            var vendor = await _vendorService.GetVendorByIdAsync(id);

            if (vendor == null)
            {
                return NotFound(new { error = "Vendor not found" });
            }

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendor by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetVendorByCode(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Vendor code is required" });
            }

            _logger.LogInformation("Getting vendor by code {Code}", code);

            var vendor = await _vendorService.GetVendorByCodeAsync(code);

            if (vendor == null)
            {
                return NotFound(new { error = "Vendor not found" });
            }

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor by code {Code}", code);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update vendor
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVendor(string id, [FromBody] Vendor vendor)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Vendor ID is required" });
            }

            if (vendor == null)
            {
                return BadRequest(new { error = "Vendor data is required" });
            }

            _logger.LogInformation("Updating vendor {Id}", id);

            vendor.Id = id;
            var success = await _vendorService.UpdateVendorAsync(vendor);

            if (success)
            {
                return Ok(vendor);
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update vendor" });
            }
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating vendor");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update vendor status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateVendorStatus(string id, [FromBody] UpdateVendorStatusRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Vendor ID is required" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Status update request is required" });
            }

            _logger.LogInformation("Updating vendor {Id} status to {Status}", id, request.Status);

            var success = await _vendorService.UpdateVendorStatusAsync(id, request.Status);

            if (success)
            {
                return Ok(new { message = "Vendor status updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update vendor status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {Id} status", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendors by status
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetVendorsByStatus(VendorStatus status, [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting vendors with status {Status}", status);

            var vendors = await _vendorService.GetVendorsByStatusAsync(status, limit);

            return Ok(new { 
                vendors,
                count = vendors.Count,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendors by status {Status}", status);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search vendors
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchVendors([FromBody] VendorSearchRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Search request is required" });
            }

            _logger.LogInformation("Searching vendors with criteria: {Criteria}", request);

            var vendors = await _vendorService.SearchVendorsAsync(request);

            return Ok(new { 
                vendors,
                count = vendors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vendors");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendor performance metrics
    /// </summary>
    [HttpGet("{id}/performance")]
    public async Task<IActionResult> GetVendorPerformance(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Vendor ID is required" });
            }

            _logger.LogInformation("Getting performance metrics for vendor {Id}", id);

            var performance = await _vendorService.GetVendorPerformanceAsync(id);

            return Ok(new { 
                performance,
                count = performance.Count,
                vendorId = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor performance {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendor compliance information
    /// </summary>
    [HttpGet("{id}/compliance")]
    public async Task<IActionResult> GetVendorCompliance(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Vendor ID is required" });
            }

            _logger.LogInformation("Getting compliance information for vendor {Id}", id);

            var compliance = await _vendorService.GetVendorComplianceAsync(id);

            return Ok(compliance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor compliance {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get vendor statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetVendorStatistics()
    {
        try
        {
            _logger.LogInformation("Getting vendor statistics");

            // This would typically use repository methods to get aggregated statistics
            // For now, return placeholder data
            var statistics = new
            {
                totalVendors = 0,
                activeVendors = 0,
                inactiveVendors = 0,
                suspendedVendors = 0,
                averageRating = 0.0,
                mostCommonService = string.Empty,
                lastUpdated = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get list of vendors for a location (matches Python functionality)
    /// </summary>
    [HttpPost("lookup")]
    public async Task<IActionResult> LookupVendors([FromBody] VendorLookupRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Vendor lookup request is required" });
            }

            _logger.LogInformation("Looking up vendors for location: {Location}", request.Location);

            var vendors = await _vendorService.LookupVendorsByLocationAsync(request.Location);

            return Ok(new
            {
                success = true,
                data = vendors,
                count = vendors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vendor lookup");
            return StatusCode(500, new
            {
                success = false,
                error = "Internal server error"
            });
        }
    }
}

/// <summary>
/// Update vendor status request
/// </summary>
public class UpdateVendorStatusRequest
{
    public VendorStatus Status { get; set; }
}

/// <summary>
/// Vendor lookup request (matches Python functionality)
/// </summary>
public class VendorLookupRequest
{
    public string Location { get; set; } = string.Empty;
    public int? Radius { get; set; }
    public string? ServiceType { get; set; }
}
