using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using IO.Elsa.Models;

namespace IO.Elsa.Infrastructure;

/// <summary>
/// Client for communicating with Elsa pricing API
/// </summary>
public interface IElsaPricingApiClient
{
    Task<ElsaPricingApiResponse> GetPriceAsync(Dictionary<string, object> requestBody);
}

/// <summary>
/// Elsa pricing API client implementation
/// </summary>
public class ElsaPricingApiClient : IElsaPricingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElsaPricingApiClient> _logger;
    private readonly string _pricingApiUrl;

    public ElsaPricingApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ElsaPricingApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pricingApiUrl = configuration["ELSA_PRICING_API"] ?? 
            throw new ArgumentException("ELSA_PRICING_API configuration is required");

        // Configure HTTP client
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IO.Elsa/1.0");
    }

    public async Task<ElsaPricingApiResponse> GetPriceAsync(Dictionary<string, object> requestBody)
    {
        try
        {
            _logger.LogInformation("Calling Elsa pricing API at {Url}", _pricingApiUrl);
            _logger.LogDebug("Request body: {RequestBody}", JsonSerializer.Serialize(requestBody));

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_pricingApiUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Elsa pricing API returned status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                throw new HttpRequestException($"Elsa pricing API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Elsa pricing API response: {Response}", responseContent);

            var apiResponse = JsonSerializer.Deserialize<ElsaPricingApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize Elsa pricing API response");
            }

            _logger.LogInformation("Successfully retrieved pricing data from Elsa API");
            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Elsa pricing API");
            throw;
        }
    }
}
