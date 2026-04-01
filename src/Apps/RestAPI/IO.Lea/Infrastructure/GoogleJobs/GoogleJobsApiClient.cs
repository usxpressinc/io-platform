using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Lea.Infrastructure.GoogleJobs;

/// <summary>
/// Google Jobs API client
/// </summary>
public interface IGoogleJobsApiClient
{
    Task<GoogleJobPosting?> PostJobAsync(GoogleJobRequest request);
    Task<bool> UpdateJobStatusAsync(string googleJobId, GoogleJobStatus status);
    Task<GoogleJobPosting?> GetJobAsync(string googleJobId);
    Task<bool> DeleteJobAsync(string googleJobId);
    Task<List<GoogleJobPosting>> ListJobsAsync(int limit = 100);
}

/// <summary>
/// Google Jobs API client implementation
/// </summary>
public class GoogleJobsApiClient : IGoogleJobsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleJobsApiClient> _logger;
    private readonly GoogleJobsApiSettings _settings;

    public GoogleJobsApiClient(
        IConfiguration configuration,
        ILogger<GoogleJobsApiClient> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("GoogleJobsApi").Get<GoogleJobsApiSettings>() ?? new GoogleJobsApiSettings();
        
        var apiKey = configuration["GoogleJobsApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("GoogleJobsApi:ApiKey", "Google Jobs API key is required");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.BaseUrl),
            Timeout = _settings.Timeout
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IO.Lea/1.0");

        _logger.LogInformation("Google Jobs API client initialized");
    }

    public async Task<GoogleJobPosting?> PostJobAsync(GoogleJobRequest request)
    {
        try
        {
            _logger.LogInformation("Posting job to Google Jobs: {Title}", request.Title);

            var response = await _httpClient.PostAsJsonAsync("/api/v1/jobs", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to post job to Google Jobs. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jobPosting = JsonSerializer.Deserialize<GoogleJobPosting>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return jobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting job to Google Jobs");
            return null;
        }
    }

    public async Task<bool> UpdateJobStatusAsync(string googleJobId, GoogleJobStatus status)
    {
        try
        {
            _logger.LogInformation("Updating Google Job {GoogleJobId} status to {Status}", googleJobId, status);

            var updateRequest = new GoogleJobStatusUpdate { Status = status };
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/jobs/{googleJobId}/status", updateRequest);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Google Job {GoogleJobId} status", googleJobId);
            return false;
        }
    }

    public async Task<GoogleJobPosting?> GetJobAsync(string googleJobId)
    {
        try
        {
            _logger.LogInformation("Getting Google Job {GoogleJobId}", googleJobId);

            var response = await _httpClient.GetAsync($"/api/v1/jobs/{googleJobId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Google Job. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jobPosting = JsonSerializer.Deserialize<GoogleJobPosting>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return jobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Google Job {GoogleJobId}", googleJobId);
            return null;
        }
    }

    public async Task<bool> DeleteJobAsync(string googleJobId)
    {
        try
        {
            _logger.LogInformation("Deleting Google Job {GoogleJobId}", googleJobId);

            var response = await _httpClient.DeleteAsync($"/api/v1/jobs/{googleJobId}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Google Job {GoogleJobId}", googleJobId);
            return false;
        }
    }

    public async Task<List<GoogleJobPosting>> ListJobsAsync(int limit = 100)
    {
        try
        {
            _logger.LogInformation("Listing Google Jobs with limit {Limit}", limit);

            var response = await _httpClient.GetAsync($"/api/v1/jobs?limit={limit}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to list Google Jobs. Status: {StatusCode}", response.StatusCode);
                return new List<GoogleJobPosting>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var jobPostings = JsonSerializer.Deserialize<List<GoogleJobPosting>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return jobPostings ?? new List<GoogleJobPosting>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Google Jobs");
            return new List<GoogleJobPosting>();
        }
    }
}

/// <summary>
/// Google Jobs API settings
/// </summary>
public class GoogleJobsApiSettings
{
    public string BaseUrl { get; set; } = "https://jobs.googleapis.com";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Google Job request
/// </summary>
public class GoogleJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Google Job posting
/// </summary>
public class GoogleJobPosting
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public GoogleJobStatus Status { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Google Job status update
/// </summary>
public class GoogleJobStatusUpdate
{
    public GoogleJobStatus Status { get; set; }
}
