using Microsoft.Extensions.Logging;
using IO.Elsa.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Elsa.Core.Pricing;

/// <summary>
/// Pricing business logic service
/// </summary>
public interface IPricingBusinessService
{
    Task<PriceCalculationResult> CalculatePriceAsync(PriceCalculationRequest request);
    Task<List<RateCard>> GetRateCardsAsync(string serviceType);
    Task<RateCard?> GetRateCardAsync(string id);
    Task<bool> UpdateRateCardAsync(RateCard rateCard);
    Task<List<PriceQuote>> GetPriceHistoryAsync(string customerId, int limit = 100);
}

/// <summary>
/// Pricing service implementation
/// </summary>
public class PricingBusinessService : IPricingBusinessService
{
    private readonly RateCardRepository _rateCardRepository;
    private readonly PriceQuoteRepository _priceQuoteRepository;
    private readonly ILogger<PricingBusinessService> _logger;

    public PricingBusinessService(
        RateCardRepository rateCardRepository,
        PriceQuoteRepository priceQuoteRepository,
        ILogger<PricingBusinessService> logger)
    {
        _rateCardRepository = rateCardRepository;
        _priceQuoteRepository = priceQuoteRepository;
        _logger = logger;
    }

    public async Task<PriceCalculationResult> CalculatePriceAsync(PriceCalculationRequest request)
    {
        try
        {
            _logger.LogInformation("Calculating price for service {ServiceType} for customer {CustomerId}", 
                request.ServiceType, request.CustomerId);

            // Get applicable rate cards
            var rateCards = await _rateCardRepository.GetByServiceTypeAsync(request.ServiceType);
            var applicableRateCard = FindApplicableRateCard(rateCards, request);

            if (applicableRateCard == null)
            {
                return new PriceCalculationResult
                {
                    Success = false,
                    ErrorMessage = "No applicable rate card found"
                };
            }

            // Calculate base price
            var basePrice = CalculateBasePrice(applicableRateCard, request);

            // Apply adjustments
            var adjustedPrice = ApplyPriceAdjustments(basePrice, request, applicableRateCard);

            // Create price quote
            var priceQuote = new PriceQuote
            {
                CustomerId = request.CustomerId,
                ServiceType = request.ServiceType,
                BasePrice = basePrice,
                AdjustedPrice = adjustedPrice,
                RateCardId = applicableRateCard.Id,
                CalculationDetails = new Dictionary<string, object>
                {
                    ["Distance"] = request.Distance,
                    ["Weight"] = request.Weight,
                    ["Volume"] = request.Volume,
                    ["ServiceLevel"] = request.ServiceLevel,
                    ["Urgency"] = request.Urgency,
                    ["RateCard"] = applicableRateCard.Name
                },
                CreatedAt = DateTime.UtcNow
            };

            await _priceQuoteRepository.CreateAsync(priceQuote);

            _logger.LogInformation("Price calculated successfully: {Price} for customer {CustomerId}", 
                adjustedPrice, request.CustomerId);

            return new PriceCalculationResult
            {
                Success = true,
                BasePrice = basePrice,
                AdjustedPrice = adjustedPrice,
                PriceQuote = priceQuote,
                RateCard = applicableRateCard
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price for customer {CustomerId}", request.CustomerId);
            return new PriceCalculationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<RateCard>> GetRateCardsAsync(string serviceType)
    {
        try
        {
            _logger.LogInformation("Getting rate cards for service type {ServiceType}", serviceType);
            return await _rateCardRepository.GetByServiceTypeAsync(serviceType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate cards for service type {ServiceType}", serviceType);
            return new List<RateCard>();
        }
    }

    public async Task<RateCard?> GetRateCardAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting rate card {Id}", id);
            return await _rateCardRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate card {Id}", id);
            return null;
        }
    }

    public async Task<bool> UpdateRateCardAsync(RateCard rateCard)
    {
        try
        {
            _logger.LogInformation("Updating rate card {Id}", rateCard.Id);
            rateCard.UpdatedAt = DateTime.UtcNow;
            return await _rateCardRepository.UpdateAsync(rateCard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate card {Id}", rateCard.Id);
            return false;
        }
    }

    public async Task<List<PriceQuote>> GetPriceHistoryAsync(string customerId, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting price history for customer {CustomerId}", customerId);
            return await _priceQuoteRepository.GetByCustomerIdAsync(customerId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for customer {CustomerId}", customerId);
            return new List<PriceQuote>();
        }
    }

    private RateCard? FindApplicableRateCard(List<RateCard> rateCards, PriceCalculationRequest request)
    {
        // Find rate card that matches criteria
        return rateCards.FirstOrDefault(rc => 
            rc.ServiceType == request.ServiceType &&
            rc.IsActive &&
            (!rc.ValidFrom.HasValue || rc.ValidFrom <= DateTime.UtcNow) &&
            (!rc.ValidTo.HasValue || rc.ValidTo >= DateTime.UtcNow) &&
            (!rc.MinWeight.HasValue || request.Weight >= rc.MinWeight) &&
            (!rc.MaxWeight.HasValue || request.Weight <= rc.MaxWeight) &&
            (!rc.MinDistance.HasValue || request.Distance >= rc.MinDistance) &&
            (!rc.MaxDistance.HasValue || request.Distance <= rc.MaxDistance));
    }

    private decimal CalculateBasePrice(RateCard rateCard, PriceCalculationRequest request)
    {
        var basePrice = 0m;

        // Distance-based pricing
        if (rateCard.PerMileRate > 0)
        {
            basePrice += request.Distance * rateCard.PerMileRate;
        }

        // Weight-based pricing
        if (rateCard.PerPoundRate > 0)
        {
            basePrice += request.Weight * rateCard.PerPoundRate;
        }

        // Volume-based pricing
        if (rateCard.PerCubicFootRate > 0)
        {
            basePrice += request.Volume * rateCard.PerCubicFootRate;
        }

        // Flat rate
        if (rateCard.FlatRate > 0)
        {
            basePrice += rateCard.FlatRate;
        }

        // Minimum charge
        if (basePrice < rateCard.MinimumCharge)
        {
            basePrice = rateCard.MinimumCharge;
        }

        return basePrice;
    }

    private decimal ApplyPriceAdjustments(decimal basePrice, PriceCalculationRequest request, RateCard rateCard)
    {
        var adjustedPrice = basePrice;

        // Service level adjustments
        if (request.ServiceLevel != null)
        {
            var serviceLevelMultiplier = request.ServiceLevel.ToLowerInvariant() switch
            {
                "standard" => 1.0m,
                "premium" => 1.25m,
                "expedited" => 1.5m,
                "white-glove" => 2.0m,
                _ => 1.0m
            };
            adjustedPrice *= serviceLevelMultiplier;
        }

        // Urgency adjustments
        if (request.Urgency != null)
        {
            var urgencyMultiplier = request.Urgency.ToLowerInvariant() switch
            {
                "normal" => 1.0m,
                "rush" => 1.3m,
                "emergency" => 2.0m,
                _ => 1.0m
            };
            adjustedPrice *= urgencyMultiplier;
        }

        // Customer discount
        if (request.CustomerDiscountPercent > 0)
        {
            adjustedPrice *= (1 - request.CustomerDiscountPercent / 100);
        }

        // Fuel surcharge
        if (rateCard.FuelSurchargePercent > 0)
        {
            adjustedPrice *= (1 + rateCard.FuelSurchargePercent / 100);
        }

        // Accessorial charges
        if (request.AccessorialCharges?.Any() == true)
        {
            adjustedPrice += request.AccessorialCharges.Sum();
        }

        return Math.Round(adjustedPrice, 2);
    }
}

/// <summary>
/// Price calculation request
/// </summary>
public class PriceCalculationRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public string? ServiceLevel { get; set; }
    public string? Urgency { get; set; }
    public decimal CustomerDiscountPercent { get; set; }
    public List<decimal>? AccessorialCharges { get; set; }
}

/// <summary>
/// Price calculation result
/// </summary>
public class PriceCalculationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal BasePrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public PriceQuote? PriceQuote { get; set; }
    public RateCard? RateCard { get; set; }
}
