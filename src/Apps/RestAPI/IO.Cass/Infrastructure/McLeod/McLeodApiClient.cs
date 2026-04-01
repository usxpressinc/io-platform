using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Cass.Infrastructure.McLeod;

/// <summary>
/// McLeod API client for carrier vetting
/// </summary>
public interface IMcLeodApiClient
{
    Task<CarrierCompliance?> GetComplianceStatusAsync(string dotNumber);
    Task<List<CarrierViolation>> GetViolationsAsync(string dotNumber);
    Task<CarrierAuditResult?> GetAuditResultsAsync(string dotNumber);
    Task<CarrierPerformanceMetrics?> GetPerformanceMetricsAsync(string dotNumber);
}

/// <summary>
/// McLeod API client implementation
/// </summary>
public class McLeodApiClient : IMcLeodApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McLeodApiClient> _logger;
    private readonly McLeodApiSettings _settings;

    public McLeodApiClient(
        IConfiguration configuration,
        ILogger<McLeodApiClient> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("McLeodApi").Get<McLeodApiSettings>() ?? new McLeodApiSettings();
        
        var apiKey = configuration["McLeodApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("McLeodApi:ApiKey", "McLeod API key is required");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.BaseUrl),
            Timeout = _settings.Timeout
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IO.Cass/1.0");
        _httpClient.DefaultRequestHeaders.Add("X-API-Version", "v1");

        _logger.LogInformation("McLeod API client initialized");
    }

    public async Task<CarrierCompliance?> GetComplianceStatusAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting compliance status for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/compliance/carriers/{dotNumber}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get compliance status. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var compliance = JsonSerializer.Deserialize<CarrierCompliance>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return compliance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compliance status for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<List<CarrierViolation>> GetViolationsAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting violations for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/violations/carriers/{dotNumber}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get violations. Status: {StatusCode}", response.StatusCode);
                return new List<CarrierViolation>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var violations = JsonSerializer.Deserialize<List<CarrierViolation>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return violations ?? new List<CarrierViolation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting violations for DOT number {DotNumber}", dotNumber);
            return new List<CarrierViolation>();
        }
    }

    public async Task<CarrierAuditResult?> GetAuditResultsAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting audit results for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/audits/carriers/{dotNumber}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get audit results. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var auditResult = JsonSerializer.Deserialize<CarrierAuditResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return auditResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit results for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<CarrierPerformanceMetrics?> GetPerformanceMetricsAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Getting performance metrics for DOT number {DotNumber}", dotNumber);

            var response = await _httpClient.GetAsync($"/api/v1/performance/carriers/{dotNumber}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get performance metrics. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<CarrierPerformanceMetrics>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics for DOT number {DotNumber}", dotNumber);
            return null;
        }
    }
}

/// <summary>
/// McLeod API settings
/// </summary>
public class McLeodApiSettings
{
    public string BaseUrl { get; set; } = "https://api.mcleod.com";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Carrier compliance status
/// </summary>
public class CarrierCompliance
{
    public string DotNumber { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public double ComplianceScore { get; set; }
    public string? LastAuditDate { get; set; }
    public List<ComplianceIssue> Issues { get; set; } = new();
    public List<ComplianceRequirement> Requirements { get; set; } = new();
}

/// <summary>
/// Compliance status enumeration
/// </summary>
public enum ComplianceStatus
{
    Compliant,
    NonCompliant,
    Pending,
    Unknown
}

/// <summary>
/// Compliance issue
/// </summary>
public class ComplianceIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public DateTime IdentifiedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool Resolved { get; set; }
    public DateTime? ResolvedDate { get; set; }
}

/// <summary>
/// Issue severity
/// </summary>
public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Compliance requirement
/// </summary>
public class ComplianceRequirement
{
    public string Requirement { get; set; } = string.Empty;
    public bool Satisfied { get; set; }
    public DateTime? SatisfiedDate { get; set; }
    public string? Evidence { get; set; }
}

/// <summary>
/// Carrier violation
/// </summary>
public class CarrierViolation
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Points { get; set; }
    public decimal FineAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ResolvedDate { get; set; }
}

/// <summary>
/// Carrier audit result
/// </summary>
public class CarrierAuditResult
{
    public string AuditId { get; set; } = string.Empty;
    public string DotNumber { get; set; } = string.Empty;
    public DateTime AuditDate { get; set; }
    public string Auditor { get; set; } = string.Empty;
    public AuditType Type { get; set; }
    public AuditResult Result { get; set; }
    public int Score { get; set; }
    public List<AuditFinding> Findings { get; set; } = new();
    public List<AuditRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Audit type
/// </summary>
public enum AuditType
{
    Safety,
    Compliance,
    Operational,
    Financial
}

/// <summary>
/// Audit result
/// </summary>
public enum AuditResult
{
    Pass,
    Fail,
    ConditionalPass,
    Unknown
}

/// <summary>
/// Audit finding
/// </summary>
public class AuditFinding
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public bool CorrectiveActionRequired { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Audit recommendation
/// </summary>
public class AuditRecommendation
{
    public string Recommendation { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Carrier performance metrics
/// </summary>
public class CarrierPerformanceMetrics
{
    public string DotNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
    public List<PerformanceTrend> Trends { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public double OnTimeDeliveryPercentage { get; set; }
    public double LoadAcceptanceRate { get; set; }
    public double CustomerSatisfactionScore { get; set; }
    public int TotalLoads { get; set; }
    public int OnTimeLoads { get; set; }
    public int DelayedLoads { get; set; }
    public int CancelledLoads { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRevenuePerLoad { get; set; }
}

/// <summary>
/// Performance trend
/// </summary>
public class PerformanceTrend
{
    public string Metric { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}
