using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Clara.Models;

/// <summary>
/// Price API request for Clara pricing
/// </summary>
public class PriceRequest
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
    
    /// <summary>
    /// Gets the stops from the extension data
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, StopInfo>? Stops
    {
        get
        {
            if (ExtensionData == null) return null;
            
            var stops = new Dictionary<string, StopInfo>();
            foreach (var kvp in ExtensionData)
            {
                if (kvp.Key.StartsWith("stops.") && kvp.Value is JsonElement element)
                {
                    var stop = JsonSerializer.Deserialize<StopInfo>(element.GetRawText());
                    if (stop != null)
                    {
                        stops[kvp.Key] = stop;
                    }
                }
            }
            return stops;
        }
    }
    
    /// <summary>
    /// Gets the load details from the extension data
    /// </summary>
    [JsonIgnore]
    public LoadDetails? LoadDetails
    {
        get
        {
            if (ExtensionData == null || !ExtensionData.TryGetValue("loadDetails", out var value))
                return null;
                
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<LoadDetails>(element.GetRawText());
            }
            return null;
        }
    }
}

/// <summary>
/// Stop information for price request
/// </summary>
public class StopInfo
{
    /// <summary>
    /// Postal code for the stop
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Late pickup date time
    /// </summary>
    public DateTime PickUpLateDateTime { get; set; }
    
    /// <summary>
    /// Early pickup date time
    /// </summary>
    public DateTime PickUpEarlyDateTime { get; set; }
}

/// <summary>
/// Load details for price request
/// </summary>
public class LoadDetails
{
    /// <summary>
    /// High value product flag
    /// </summary>
    public bool Hvp { get; set; }
    
    /// <summary>
    /// Hazmat flag
    /// </summary>
    public bool Hazmat { get; set; }
    
    /// <summary>
    /// Load identifier
    /// </summary>
    public string LoadId { get; set; } = string.Empty;
    
    /// <summary>
    /// Temperature protection flag
    /// </summary>
    public bool TempProtect { get; set; }
    
    /// <summary>
    /// Equipment type
    /// </summary>
    public string EquipmentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Request source
    /// </summary>
    public string RequestdSource { get; set; } = string.Empty;
    
    /// <summary>
    /// Tanker endorsement flag
    /// </summary>
    public bool TankerEndorsement { get; set; }
}

/// <summary>
/// Price API response from Clara
/// </summary>
public class PriceResponse
{
    /// <summary>
    /// Price quote amount
    /// </summary>
    public decimal? Amount { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    public string? Currency { get; set; }
    
    /// <summary>
    /// Quote identifier
    /// </summary>
    public string? QuoteId { get; set; }
    
    /// <summary>
    /// Response status
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Response errors if any
    /// </summary>
    public List<PriceError>? Errors { get; set; }
}

/// <summary>
/// Price API error information
/// </summary>
public class PriceError
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Error details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}
