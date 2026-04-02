using System.Text.Json;
using IO.Clara.Models;
using IO.Platform.Common.Core.Exceptions;

namespace IO.Clara.Infrastructure.Highway;

/// <summary>
/// Highway API client for Clara carrier validation
/// </summary>
public interface IHighwayApiClient
{
    Task<HighwayCarrierData?> GetHighwayDataAsync(string dotNumber, string mcNumber);
}

/// <summary>
/// Highway API client implementation for Clara
/// </summary>
public class HighwayApiClient : IHighwayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HighwayApiClient> _logger;
    private readonly HighwayApiSettings _settings;

    public HighwayApiClient(
        IConfiguration configuration,
        ILogger<HighwayApiClient> logger)
    {
        this._logger = logger;
        this._settings = configuration.GetSection("HighwayApi").Get<HighwayApiSettings>() ?? new HighwayApiSettings();
        
        var apiKey = configuration["HighwayApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("HighwayApi:ApiKey", "Highway API key is required");
        }

        this._httpClient = new HttpClient
        {
            BaseAddress = new Uri(this._settings.BaseUrl),
            Timeout = this._settings.Timeout
        };

        this._httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        this._httpClient.DefaultRequestHeaders.Add("User-Agent", "IO.Clara/1.0");

        this._logger.LogInformation("Highway API client initialized for Clara");
    }

    public async Task<HighwayCarrierData?> GetHighwayDataAsync(string dotNumber, string mcNumber)
    {
        try
        {
            this._logger.LogInformation("Getting Highway data for DOT: {DotNumber}, MC: {McNumber}", dotNumber, mcNumber);

            // Use DOT number as primary identifier, MC as fallback (like Python)
            var identifier = !string.IsNullOrEmpty(dotNumber) ? dotNumber : mcNumber;
            
            var response = await this._httpClient.GetAsync($"/api/v1/carriers/{identifier}");
            
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Failed to get Highway data. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var carrierData = JsonSerializer.Deserialize<HighwayCarrierData>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return carrierData;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting Highway data for DOT: {DotNumber}, MC: {McNumber}", dotNumber, mcNumber);
            return null;
        }
    }
}

/// <summary>
/// Highway API settings
/// </summary>
public class HighwayApiSettings
{
    public string BaseUrl { get; set; } = "https://api.highway.com";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
