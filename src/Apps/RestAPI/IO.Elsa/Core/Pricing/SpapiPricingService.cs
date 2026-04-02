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
    Task<SpapiPricingResponse> CalculatePriceAsync(SpapiPricingRequest request);
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
        this._elsaApiClient = elsaApiClient;
        this._logger = logger;
    }

    public async Task<SpapiPricingResponse> LookupPriceAsync(Dictionary<string, object> requestBody)
    {
        try
        {
            this._logger.LogInformation("Starting SPAPI price lookup");

            // Process stops from the request body (matching Python implementation)
            var processedRequest = this.ProcessStopsInRequest(requestBody);

            this._logger.LogDebug("Processed SPAPI request: {Request}", JsonSerializer.Serialize(processedRequest));

            // Call Elsa pricing API
            var apiResponse = await this._elsaApiClient.GetPriceAsync(processedRequest);

            this._logger.LogDebug("Elsa API response: {Response}", JsonSerializer.Serialize(apiResponse));

            // Extract pricing data
            var pricingData = apiResponse.Data?.Pricing ??
            [
            ];
            
            // Find the "01-BROKERAGE" pricing item (matching Python implementation)
            var brokeragePriceItem = pricingData.FirstOrDefault(x => x.Name == "01-BROKERAGE");
            
            if (brokeragePriceItem?.Price == null)
            {
                this._logger.LogWarning("No brokerage pricing item found in Elsa API response");
                return new SpapiPricingResponse
                {
                    Error = "No brokerage pricing available"
                };
            }

            this._logger.LogDebug("Brokerage price item: {PriceItem}", JsonSerializer.Serialize(brokeragePriceItem));

            // Create response
            var result = new SpapiPricingResponse
            {
                AllInPrice = brokeragePriceItem.Price.AllInPrice,
                Distance = brokeragePriceItem.Price.Distance,
                BasePrice = brokeragePriceItem.Price.Cost
            };

            this._logger.LogInformation("SPAPI price lookup completed successfully. All-in price: {AllInPrice}, Distance: {Distance}", 
                result.AllInPrice, result.Distance);

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during SPAPI price lookup");
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
                            stopData[property.Name] = this.ParseObjectValue(property.Value);
                        }
                    }
                    else if (kvp.Value is Dictionary<string, object> dict)
                    {
                        foreach (var item in dict)
                        {
                            stopData[item.Key] = this.ParseObjectValue(item.Value);
                        }
                    }

                    stops.Add(stopData);
                }
            }
            else
            {
                // Process non-stop data
                processedRequest[kvp.Key] = this.ParseObjectValue(kvp.Value);
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
            return this.ParseJsonElement(jsonElement);
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
                parsedDict[kvp.Key] = this.ParseObjectValue(kvp.Value);
            }
            return parsedDict;
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(this.ParseObjectValue(item));
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
                return this.ParseObjectValue(element.GetString() ?? string.Empty);
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
                    dict[property.Name] = this.ParseJsonElement(property.Value);
                }
                return dict;
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(this.ParseJsonElement(item));
                }
                return list;
            default:
                return element.GetRawText();
        }
    }

    public async Task<SpapiPricingResponse> CalculatePriceAsync(SpapiPricingRequest request)
    {
        try
        {
            this._logger.LogInformation("Starting price calculation for {StopCount} stops", request.Stops?.Count ?? 0);

            // Convert SpapiPricingRequest to dictionary format expected by the API
            var requestBody = new Dictionary<string, object>();
            
            if (request.AdditionalData != null)
            {
                foreach (var kvp in request.AdditionalData)
                {
                    requestBody[kvp.Key] = this.ParseObjectValue(kvp.Value);
                }
            }

            // Add stops to request
            if (request.Stops != null)
            {
                for (int i = 0; i < request.Stops.Count; i++)
                {
                    var stop = request.Stops[i];
                    var stopKey = $"stops.{i}";
                    requestBody[stopKey] = new Dictionary<string, object>
                    {
                        ["seq"] = i,
                        // Add additional stop data if present
                        ["additionalData"] = stop.AdditionalData ?? new Dictionary<string, object>()
                    };
                }
            }

            // Call the existing LookupPriceAsync method
            return await this.LookupPriceAsync(requestBody);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during price calculation");
            return new SpapiPricingResponse
            {
                Error = ex.Message
            };
        }
    }
}
