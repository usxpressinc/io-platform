using IO.Elsa.Infrastructure;
using IO.Elsa.Models;
using System.Text.Json;

namespace IO.Elsa.Core.Pricing;

/// <summary>
/// SPAPI pricing business logic service
/// </summary>
public interface ISpapiPricingService
{
    Task<SpapiPricingResponse> LookupPriceAsync(Dictionary<string, object> requestBody);
}

/// <summary>
/// SPAPI pricing service implementation
/// </summary>
public class SpapiPricingService : ISpapiPricingService
{
    private readonly IElsaPricingApiClient _elsaApiClient;
    private readonly ILogger<SpapiPricingService> _logger;

    public SpapiPricingService(
        IElsaPricingApiClient elsaApiClient,
        ILogger<SpapiPricingService> logger)
    {
        _elsaApiClient = elsaApiClient;
        _logger = logger;
    }

    public async Task<SpapiPricingResponse> LookupPriceAsync(Dictionary<string, object> requestBody)
    {
        try
        {
            _logger.LogInformation("Starting SPAPI price lookup");

            // Process stops from the request body (matching Python implementation)
            var processedRequest = ProcessStopsInRequest(requestBody);
            
            _logger.LogDebug("Processed SPAPI request: {Request}", JsonSerializer.Serialize(processedRequest));

            // Call Elsa pricing API
            var apiResponse = await _elsaApiClient.GetPriceAsync(processedRequest);
            
            _logger.LogDebug("Elsa API response: {Response}", JsonSerializer.Serialize(apiResponse));

            // Extract pricing data
            var pricingData = apiResponse.Data?.Pricing ?? new List<ElsaPricingItem>();
            
            // Find the "01-BROKERAGE" pricing item (matching Python implementation)
            var brokeragePriceItem = pricingData.FirstOrDefault(x => x.Name == "01-BROKERAGE");
            
            if (brokeragePriceItem?.Price == null)
            {
                _logger.LogWarning("No brokerage pricing item found in Elsa API response");
                return new SpapiPricingResponse
                {
                    Error = "No brokerage pricing available"
                };
            }

            _logger.LogDebug("Brokerage price item: {PriceItem}", JsonSerializer.Serialize(brokeragePriceItem));

            // Create response
            var result = new SpapiPricingResponse
            {
                AllInPrice = brokeragePriceItem.Price.AllInPrice,
                Distance = brokeragePriceItem.Price.Distance,
                BasePrice = brokeragePriceItem.Price.Cost
            };

            _logger.LogInformation("SPAPI price lookup completed successfully. All-in price: {AllInPrice}, Distance: {Distance}", 
                result.AllInPrice, result.Distance);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SPAPI price lookup");
            return new SpapiPricingResponse
            {
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Process stops in the request body (matching Python implementation)
    /// </summary>
    private Dictionary<string, object> ProcessStopsInRequest(Dictionary<string, object> requestBody)
    {
        var processedRequest = new Dictionary<string, object>();
        var stops = new List<Dictionary<string, object>>();

        // Extract stops and process other data
        foreach (var kvp in requestBody)
        {
            if (kvp.Key.StartsWith("stops."))
            {
                // Parse sequence number from key (e.g., "stops.0", "stops.1")
                var keyParts = kvp.Key.Split('.', 2);
                if (keyParts.Length == 2 && int.TryParse(keyParts[1], out var seq))
                {
                    var stopData = new Dictionary<string, object>
                    {
                        ["seq"] = seq
                    };

                    // Add the stop data
                    if (kvp.Value is JsonElement jsonElement)
                    {
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            stopData[property.Name] = ParseObjectValue(property.Value);
                        }
                    }
                    else if (kvp.Value is Dictionary<string, object> dict)
                    {
                        foreach (var item in dict)
                        {
                            stopData[item.Key] = ParseObjectValue(item.Value);
                        }
                    }

                    stops.Add(stopData);
                }
            }
            else
            {
                // Process non-stop data
                processedRequest[kvp.Key] = ParseObjectValue(kvp.Value);
            }
        }

        // Sort stops by sequence number and add to request
        stops.Sort((x, y) => ((int)x["seq"]).CompareTo((int)y["seq"]));
        processedRequest["stops"] = stops;

        return processedRequest;
    }

    /// <summary>
    /// Parse object values (matching Python implementation's parse_object function)
    /// </summary>
    private object ParseObjectValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return ParseJsonElement(jsonElement);
        }

        if (value is string stringValue)
        {
            // Handle boolean strings
            if (stringValue.ToLower() == "false")
                return false;
            if (stringValue.ToLower() == "true")
                return true;

            // Try to parse as integer
            if (int.TryParse(stringValue, out var intValue))
                return intValue;

            // Try to parse as double
            if (double.TryParse(stringValue, out var doubleValue))
                return doubleValue;

            // Return as string if no other parsing works
            return stringValue;
        }

        if (value is Dictionary<string, object> dict)
        {
            var parsedDict = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                parsedDict[kvp.Key] = ParseObjectValue(kvp.Value);
            }
            return parsedDict;
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(ParseObjectValue(item));
            }
            return list;
        }

        return value;
    }

    /// <summary>
    /// Parse JsonElement values
    /// </summary>
    private object ParseJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return ParseObjectValue(element.GetString() ?? string.Empty);
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ParseJsonElement(property.Value);
                }
                return dict;
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ParseJsonElement(item));
                }
                return list;
            default:
                return element.GetRawText();
        }
    }
}
