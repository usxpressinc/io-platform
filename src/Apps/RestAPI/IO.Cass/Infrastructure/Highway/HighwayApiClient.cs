using System.Text.Json;

namespace IO.Cass.Infrastructure.Highway;

/// <summary>
/// Highway API client for carrier vetting
/// </summary>
public interface IHighwayApiClient
{
    Task<CarrierInfo?> GetCarrierInfoAsync(string dotNumber);
    Task<List<CarrierInfo>> SearchCarriersAsync(CarrierSearchRequest request);
    Task<CarrierSafetyRating?> GetSafetyRatingAsync(string dotNumber);
    Task<CarrierInsuranceInfo?> GetInsuranceInfoAsync(string dotNumber);
    Task<CarrierOperatingAuthority?> GetOperatingAuthorityAsync(string dotNumber);
}

/// <summary>
/// Highway API client implementation
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
        _logger = logger;
        _settings = configuration.GetSection("HighwayApi").Get<HighwayApiSettings>() ?? new HighwayApiSettings();
        
        var apiKey = configuration["HighwayApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("HighwayApi:ApiKey", "Highway API key is required");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.BaseUrl),
            Timeout = _settings.Timeout
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IO.Cass/1.0");

        _logger.LogInformation("Highway API client initialized");
    }

    public async Task<CarrierInfo?> GetCarrierInfoAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting carrier info for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/carriers/{dotNumber}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get carrier info. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var carrierInfo = JsonSerializer.Deserialize<CarrierInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return carrierInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier info for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<List<CarrierInfo>> SearchCarriersAsync(CarrierSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching carriers with criteria: {Criteria}", request);

            var queryParams = new List<string>
            {
                $"name={Uri.EscapeDataString(request.Name ?? "")}",
                $"city={Uri.EscapeDataString(request.City ?? "")}",
                $"state={Uri.EscapeDataString(request.State ?? "")}",
                $"mcNumber={Uri.EscapeDataString(request.McNumber ?? "")}",
                $"limit={request.Limit}"
            };

            var queryString = string.Join("&", queryParams.Where(q => !q.EndsWith("=")));
            var response = await _httpClient.GetAsync($"/api/v1/carriers/search?{queryString}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search carriers. Status: {StatusCode}", response.StatusCode);
                return new List<CarrierInfo>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var carriers = JsonSerializer.Deserialize<List<CarrierInfo>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return carriers ?? new List<CarrierInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching carriers");
            return new List<CarrierInfo>();
        }
    }

    public async Task<CarrierSafetyRating?> GetSafetyRatingAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting safety rating for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/carriers/{dotNumber}/safety");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get safety rating. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var safetyRating = JsonSerializer.Deserialize<CarrierSafetyRating>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return safetyRating;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting safety rating for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<CarrierInsuranceInfo?> GetInsuranceInfoAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting insurance info for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/carriers/{dotNumber}/insurance");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get insurance info. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var insuranceInfo = JsonSerializer.Deserialize<CarrierInsuranceInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return insuranceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insurance info for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<CarrierOperatingAuthority?> GetOperatingAuthorityAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting operating authority for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/carriers/{dotNumber}/authority");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get operating authority. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var authority = JsonSerializer.Deserialize<CarrierOperatingAuthority>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return authority;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting operating authority for DOT number {DotNumber}", dotNumber);
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
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Carrier information
/// </summary>
public class CarrierInfo
{
    public string DotNumber { get; set; } = string.Empty;
    public string McNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DbaName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Carrier search request
/// </summary>
public class CarrierSearchRequest
{
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? McNumber { get; set; }
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Carrier safety rating
/// </summary>
public class CarrierSafetyRating
{
    public string DotNumber { get; set; } = string.Empty;
    public string OverallRating { get; set; } = string.Empty;
    public double? Score { get; set; }
    public string? RatingDate { get; set; }
    public List<SafetyViolation> Violations { get; set; } = new();
    public List<SafetyInspection> Inspections { get; set; } = new();
}

/// <summary>
/// Safety violation
/// </summary>
public class SafetyViolation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Points { get; set; }
}

/// <summary>
/// Safety inspection
/// </summary>
public class SafetyInspection
{
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Result { get; set; } = string.Empty;
    public int? Score { get; set; }
}

/// <summary>
/// Carrier insurance information
/// </summary>
public class CarrierInsuranceInfo
{
    public string DotNumber { get; set; } = string.Empty;
    public bool HasInsurance { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? PolicyNumber { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal? CoverageAmount { get; set; }
    public List<InsuranceType> CoverageTypes { get; set; } = new();
}

/// <summary>
/// Insurance type
/// </summary>
public class InsuranceType
{
    public string Type { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }
    public string? Deductible { get; set; }
}

/// <summary>
/// Carrier operating authority
/// </summary>
public class CarrierOperatingAuthority
{
    public string DotNumber { get; set; } = string.Empty;
    public List<OperatingAuthorityType> AuthorityTypes { get; set; } = new();
    public DateTime? GrantedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Operating authority type
/// </summary>
public class OperatingAuthorityType
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? GrantedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
