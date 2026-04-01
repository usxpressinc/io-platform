using System.Text.Json.Serialization;

namespace IO.Elsa.Models;

/// <summary>
/// SPAPI pricing lookup request
/// </summary>
public class SpapiPricingRequest
{
    /// <summary>
    /// Dictionary containing pricing request parameters including stops
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// SPAPI pricing lookup response
/// </summary>
public class SpapiPricingResponse
{
    /// <summary>
    /// All in price for the request
    /// </summary>
    public double AllInPrice { get; set; }

    /// <summary>
    /// Total distance for the order in miles
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Base price for the request
    /// </summary>
    public double BasePrice { get; set; }

    /// <summary>
    /// Error message if pricing lookup failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Stop information for SPAPI request
/// </summary>
public class SpapiStop
{
    /// <summary>
    /// Sequence number of the stop
    /// </summary>
    public int Seq { get; set; }

    /// <summary>
    /// Additional stop data
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Elsa pricing API response structure
/// </summary>
public class ElsaPricingApiResponse
{
    /// <summary>
    /// Response data
    /// </summary>
    public ElsaPricingData? Data { get; set; }
}

/// <summary>
/// Elsa pricing data
/// </summary>
public class ElsaPricingData
{
    /// <summary>
    /// Pricing information
    /// </summary>
    public List<ElsaPricingItem>? Pricing { get; set; }
}

/// <summary>
/// Individual pricing item from Elsa API
/// </summary>
public class ElsaPricingItem
{
    /// <summary>
    /// Name of the pricing item
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Price details
    /// </summary>
    public ElsaPriceDetails? Price { get; set; }
}

/// <summary>
/// Price details from Elsa API
/// </summary>
public class ElsaPriceDetails
{
    /// <summary>
    /// All in price
    /// </summary>
    public double AllInPrice { get; set; }

    /// <summary>
    /// Distance in miles
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Base cost
    /// </summary>
    public double Cost { get; set; }
}
