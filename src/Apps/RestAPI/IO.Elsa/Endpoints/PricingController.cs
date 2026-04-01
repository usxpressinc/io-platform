using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Elsa.Core.Pricing;
using IO.Elsa.Infrastructure.Data;
using IO.Elsa.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Elsa.Endpoints;

/// <summary>
/// Pricing endpoints for IO.Elsa service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingBusinessService _pricingService;
    private readonly ISpapiPricingService _spapiPricingService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IPricingBusinessService pricingService,
        ISpapiPricingService spapiPricingService,
        ILogger<PricingController> logger)
    {
        _pricingService = pricingService;
        _spapiPricingService = spapiPricingService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate price for a service
    /// </summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePrice([FromBody] PriceCalculationRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Price calculation request is required" });
            }

            _logger.LogInformation("Calculating price for customer {CustomerId} service {ServiceType}", 
                request.CustomerId, request.ServiceType);

            var result = await _pricingService.CalculatePriceAsync(request);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    basePrice = result.BasePrice,
                    adjustedPrice = result.AdjustedPrice,
                    priceQuote = result.PriceQuote,
                    rateCard = result.RateCard
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
            _logger.LogError(ex, "Error calculating price");
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get rate cards for a service type
    /// </summary>
    [HttpGet("rate-cards/{serviceType}")]
    public async Task<IActionResult> GetRateCards(string serviceType)
    {
        try
        {
            if (string.IsNullOrEmpty(serviceType))
            {
                return BadRequest(new { error = "Service type is required" });
            }

            _logger.LogInformation("Getting rate cards for service type {ServiceType}", serviceType);

            var rateCards = await _pricingService.GetRateCardsAsync(serviceType);

            return Ok(new { 
                rateCards,
                count = rateCards.Count,
                serviceType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate cards for service type {ServiceType}", serviceType);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get a specific rate card
    /// </summary>
    [HttpGet("rate-cards/{id}")]
    public async Task<IActionResult> GetRateCard(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Rate card ID is required" });
            }

            _logger.LogInformation("Getting rate card {Id}", id);

            var rateCard = await _pricingService.GetRateCardAsync(id);

            if (rateCard == null)
            {
                return NotFound(new { error = "Rate card not found" });
            }

            return Ok(rateCard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate card {Id}", id);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Update a rate card
    /// </summary>
    [HttpPut("rate-cards/{id}")]
    public async Task<IActionResult> UpdateRateCard(string id, [FromBody] RateCard rateCard)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Rate card ID is required" });
            }

            if (rateCard == null)
            {
                return BadRequest(new { error = "Rate card data is required" });
            }

            _logger.LogInformation("Updating rate card {Id}", id);

            rateCard.Id = id;
            var success = await _pricingService.UpdateRateCardAsync(rateCard);

            if (success)
            {
                return Ok(new { message = "Rate card updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update rate card" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate card {Id}", id);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get price history for a customer
    /// </summary>
    [HttpGet("history/{customerId}")]
    public async Task<IActionResult> GetPriceHistory(string customerId, [FromQuery] int limit = 100)
    {
        try
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest(new { error = "Customer ID is required" });
            }

            _logger.LogInformation("Getting price history for customer {CustomerId}", customerId);

            var priceHistory = await _pricingService.GetPriceHistoryAsync(customerId, limit);

            return Ok(new { 
                priceHistory,
                count = priceHistory.Count,
                customerId,
                limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for customer {CustomerId}", customerId);
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Get pricing statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetPricingStatistics()
    {
        try
        {
            _logger.LogInformation("Getting pricing statistics");

            // This would typically use repository methods to get aggregated statistics
            // For now, return placeholder data
            var statistics = new
            {
                totalRateCards = 0,
                activeRateCards = 0,
                totalPriceQuotes = 0,
                averagePrice = 0.0,
                mostUsedServiceType = string.Empty,
                lastUpdated = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing statistics");
            return StatusCode(500, new { 
                error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// SPAPI price lookup endpoint - matches Python implementation
    /// </summary>
    [HttpPost("spapi/lookup")]
    public async Task<IActionResult> LookupSpapiPrice([FromBody] Dictionary<string, object> requestBody)
    {
        try
        {
            if (requestBody == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("SPAPI price lookup request received");

            var result = await _spapiPricingService.LookupPriceAsync(requestBody);

            if (result.Error != null)
            {
                return BadRequest(new { 
                    success = false,
                    error = result.Error
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    allInPrice = result.AllInPrice,
                    basePrice = result.BasePrice,
                    distance = result.Distance
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SPAPI price lookup");
            return StatusCode(500, new { 
                success = false,
                error = "Internal server error"
            });
        }
    }
}
