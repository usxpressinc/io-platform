using Microsoft.Extensions.Logging;
using IO.Cass.Infrastructure.Highway;
using IO.Cass.Infrastructure.McLeod;
using IO.Cass.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Cass.Core.Carriers;

/// <summary>
/// Carrier vetting business logic service
/// </summary>
public interface ICarrierVettingService
{
    Task<CarrierVettingResult> VetCarrierAsync(string dotNumber);
    Task<CarrierVettingResult> ReVetCarrierAsync(string dotNumber);
    Task<List<Carrier>> GetCarriersByVettingStatusAsync(VettingStatus status, int limit = 100);
    Task<Carrier?> GetCarrierByDotNumberAsync(string dotNumber);
    Task<List<Carrier>> SearchCarriersAsync(CarrierSearchRequest request);
    Task<bool> UpdateCarrierVettingStatusAsync(string dotNumber, VettingStatus status, double score);
}

/// <summary>
/// Carrier vetting service implementation
/// </summary>
public class CarrierVettingService : ICarrierVettingService
{
    private readonly IHighwayApiClient _highwayApiClient;
    private readonly IMcLeodApiClient _mcleodApiClient;
    private readonly CarrierRepository _carrierRepository;
    private readonly ILogger<CarrierVettingService> _logger;

    public CarrierVettingService(
        IHighwayApiClient highwayApiClient,
        IMcLeodApiClient mcleodApiClient,
        CarrierRepository carrierRepository,
        ILogger<CarrierVettingService> logger)
    {
        _highwayApiClient = highwayApiClient;
        _mcleodApiClient = mcleodApiClient;
        _carrierRepository = carrierRepository;
        _logger = logger;
    }

    public async Task<CarrierVettingResult> VetCarrierAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Starting carrier vetting for DOT number {DotNumber}", dotNumber);

            // Check if carrier already exists
            var existingCarrier = await _carrierRepository.GetByDotNumberAsync(dotNumber);
            
            // Get data from Highway API
            var highwayData = await _highwayApiClient.GetCarrierInfoAsync(dotNumber);
            var safetyRating = await _highwayApiClient.GetSafetyRatingAsync(dotNumber);
            var insuranceInfo = await _highwayApiClient.GetInsuranceInfoAsync(dotNumber);
            var operatingAuthority = await _highwayApiClient.GetOperatingAuthorityAsync(dotNumber);

            // Get data from McLeod API
            var complianceStatus = await _mcleodApiClient.GetComplianceStatusAsync(dotNumber);
            var violations = await _mcleodApiClient.GetViolationsAsync(dotNumber);
            var auditResults = await _mcleodApiClient.GetAuditResultsAsync(dotNumber);
            var performanceMetrics = await _mcleodApiClient.GetPerformanceMetricsAsync(dotNumber);

            // Calculate vetting score
            var vettingScore = CalculateVettingScore(safetyRating, insuranceInfo, complianceStatus, violations, auditResults, performanceMetrics);

            // Determine vetting status
            var vettingStatus = DetermineVettingStatus(vettingScore, safetyRating, insuranceInfo, complianceStatus);

            // Create or update carrier record
            Carrier carrier;
            if (existingCarrier != null)
            {
                carrier = existingCarrier;
                UpdateCarrierData(carrier, highwayData, safetyRating, insuranceInfo, operatingAuthority, complianceStatus, violations, auditResults, performanceMetrics);
                await _carrierRepository.UpdateAsync(carrier);
            }
            else
            {
                carrier = CreateCarrier(dotNumber, highwayData, safetyRating, insuranceInfo, operatingAuthority, complianceStatus, violations, auditResults, performanceMetrics);
                carrier = await _carrierRepository.CreateAsync(carrier);
            }

            // Update vetting status
            await _carrierRepository.UpdateVettingStatusAsync(dotNumber, vettingStatus, vettingScore);

            _logger.LogInformation("Carrier vetting completed for DOT number {DotNumber}. Status: {Status}, Score: {Score}", 
                dotNumber, vettingStatus, vettingScore);

            return new CarrierVettingResult
            {
                Success = true,
                Carrier = carrier,
                VettingStatus = vettingStatus,
                VettingScore = vettingScore,
                VettingDate = DateTime.UtcNow,
                HighwayData = highwayData,
                SafetyRating = safetyRating,
                InsuranceInfo = insuranceInfo,
                ComplianceStatus = complianceStatus,
                Violations = violations,
                AuditResults = auditResults,
                PerformanceMetrics = performanceMetrics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during carrier vetting for DOT number {DotNumber}", dotNumber);
            return new CarrierVettingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                VettingDate = DateTime.UtcNow
            };
        }
    }

    public async Task<CarrierVettingResult> ReVetCarrierAsync(string dotNumber)
    {
        try
        {
            _logger.LogInformation("Starting carrier re-vetting for DOT number {DotNumber}", dotNumber);

            // Check if carrier exists
            var existingCarrier = await _carrierRepository.GetByDotNumberAsync(dotNumber);
            if (existingCarrier == null)
            {
                return new CarrierVettingResult
                {
                    Success = false,
                    ErrorMessage = "Carrier not found",
                    VettingDate = DateTime.UtcNow
                };
            }

            // Perform full vetting
            var result = await VetCarrierAsync(dotNumber);
            
            // Add re-vetting specific information
            result.PreviousVettingStatus = existingCarrier.VettingStatus;
            result.PreviousVettingScore = existingCarrier.VettingScore;
            result.PreviousVettingDate = existingCarrier.LastVettedAt;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during carrier re-vetting for DOT number {DotNumber}", dotNumber);
            return new CarrierVettingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                VettingDate = DateTime.UtcNow
            };
        }
    }

    public async Task<List<Carrier>> GetCarriersByVettingStatusAsync(VettingStatus status, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting carriers with vetting status {Status}", status);
            
            var carriers = await _carrierRepository.GetByVettingStatusAsync(status, limit);
            
            _logger.LogInformation("Found {CarrierCount} carriers with vetting status {Status}", carriers.Count, status);
            
            return carriers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carriers by vetting status {Status}", status);
            return new List<Carrier>();
        }
    }

    public async Task<Carrier?> GetCarrierByDotNumberAsync(string dotNumber)
    {
        try
        {
            _logger.LogDebug("Getting carrier by DOT number {DotNumber}", dotNumber);
            
            return await _carrierRepository.GetByDotNumberAsync(dotNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier by DOT number {DotNumber}", dotNumber);
            return null;
        }
    }

    public async Task<List<Carrier>> SearchCarriersAsync(CarrierSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching carriers with criteria: {Criteria}", request);
            
            var carriers = await _carrierRepository.SearchAsync(request);
            
            _logger.LogInformation("Found {CarrierCount} carriers matching criteria", carriers.Count);
            
            return carriers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching carriers");
            return new List<Carrier>();
        }
    }

    public async Task<bool> UpdateCarrierVettingStatusAsync(string dotNumber, VettingStatus status, double score)
    {
        try
        {
            _logger.LogInformation("Updating vetting status for DOT number {DotNumber} to {Status} with score {Score}", 
                dotNumber, status, score);
            
            return await _carrierRepository.UpdateVettingStatusAsync(dotNumber, status, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vetting status for DOT number {DotNumber}", dotNumber);
            return false;
        }
    }

    private double CalculateVettingScore(
        HighwayCarrierData? highwayData,
        CarrierSafetyRating? safetyRating,
        CarrierInsuranceInfo? insuranceInfo,
        CarrierCompliance? complianceStatus,
        List<CarrierViolation> violations,
        CarrierAuditResult? auditResults,
        CarrierPerformanceMetrics? performanceMetrics)
    {
        var score = 100.0;

        // Safety rating impact (30% weight)
        if (safetyRating != null)
        {
            if (safetyRating.OverallRating == "Satisfactory")
                score -= 0;
            else if (safetyRating.OverallRating == "Conditional")
                score -= 15;
            else if (safetyRating.OverallRating == "Unsatisfactory")
                score -= 30;
        }

        // Insurance impact (20% weight)
        if (insuranceInfo != null)
        {
            if (!insuranceInfo.HasInsurance)
                score -= 20;
            else if (insuranceInfo.ExpirationDate.HasValue && insuranceInfo.ExpirationDate < DateTime.UtcNow.AddDays(30))
                score -= 10;
        }

        // Compliance impact (30% weight)
        if (complianceStatus != null)
        {
            score -= (100 - complianceStatus.ComplianceScore) * 0.3;
        }

        // Violations impact (15% weight)
        if (violations.Any())
        {
            var totalPoints = violations.Sum(v => v.Points);
            score -= Math.Min(totalPoints * 0.5, 15);
        }

        // Performance impact (5% weight)
        if (performanceMetrics?.Metrics != null)
        {
            var metrics = performanceMetrics.Metrics;
            if (metrics.OnTimeDeliveryPercentage < 90)
                score -= 5;
            if (metrics.CustomerSatisfactionScore < 3.0)
                score -= 3;
        }

        return Math.Max(0, Math.Min(100, score));
    }

    private VettingStatus DetermineVettingStatus(
        double score,
        CarrierSafetyRating? safetyRating,
        CarrierInsuranceInfo? insuranceInfo,
        CarrierCompliance? complianceStatus)
    {
        if (score >= 80 && 
            (safetyRating?.OverallRating == "Satisfactory" || safetyRating == null) &&
            (insuranceInfo?.HasInsurance == true || insuranceInfo == null) &&
            (complianceStatus?.Status == ComplianceStatus.Compliant || complianceStatus == null))
        {
            return VettingStatus.Approved;
        }
        else if (score >= 60)
        {
            return VettingStatus.RequiresReview;
        }
        else if (score < 40 || 
                 safetyRating?.OverallRating == "Unsatisfactory" ||
                 (insuranceInfo?.HasInsurance == false) ||
                 complianceStatus?.Status == ComplianceStatus.NonCompliant)
        {
            return VettingStatus.Rejected;
        }
        else
        {
            return VettingStatus.RequiresReview;
        }
    }

    private Carrier CreateCarrier(
        string dotNumber,
        HighwayCarrierData? highwayData,
        CarrierSafetyRating? safetyRating,
        CarrierInsuranceInfo? insuranceInfo,
        CarrierOperatingAuthority? operatingAuthority,
        CarrierCompliance? complianceStatus,
        List<CarrierViolation> violations,
        CarrierAuditResult? auditResults,
        CarrierPerformanceMetrics? performanceMetrics)
    {
        return new Carrier
        {
            DotNumber = dotNumber,
            McNumber = highwayData?.McNumber ?? string.Empty,
            LegalName = highwayData?.LegalName ?? string.Empty,
            DbaName = highwayData?.DbaName ?? string.Empty,
            Address = highwayData?.Address ?? string.Empty,
            City = highwayData?.City ?? string.Empty,
            State = highwayData?.State ?? string.Empty,
            ZipCode = highwayData?.ZipCode ?? string.Empty,
            Phone = highwayData?.Phone ?? string.Empty,
            Email = highwayData?.Email ?? string.Empty,
            Status = CarrierStatus.Active,
            HighwayData = highwayData != null ? new HighwayCarrierData
            {
                SafetyRating = safetyRating?.OverallRating ?? string.Empty,
                SafetyScore = safetyRating?.Score,
                InsuranceProvider = insuranceInfo?.InsuranceProvider,
                InsuranceExpiration = insuranceInfo?.ExpirationDate,
                OperatingAuthorities = operatingAuthority?.AuthorityTypes.Select(a => new OperatingAuthority
                {
                    Type = a.Type,
                    Description = a.Description,
                    GrantedDate = a.GrantedDate,
                    ExpirationDate = a.ExpirationDate
                }).ToList() ?? new(),
                RetrievedAt = DateTime.UtcNow
            } : null,
            McLeodData = complianceStatus != null ? new McLeodCarrierData
            {
                ComplianceStatus = complianceStatus.Status,
                ComplianceScore = complianceStatus.ComplianceScore,
                LastAuditDate = complianceStatus.LastAuditDate,
                ComplianceIssues = complianceStatus.Issues.Select(i => new ComplianceIssue
                {
                    Type = i.Type,
                    Description = i.Description,
                    Severity = i.Severity,
                    IdentifiedDate = i.IdentifiedDate,
                    DueDate = i.DueDate,
                    Resolved = i.Resolved,
                    ResolvedDate = i.ResolvedDate
                }).ToList(),
                Violations = violations.Select(v => new PerformanceViolation
                {
                    Type = v.Type,
                    Description = v.Description,
                    Date = v.Date,
                    Points = v.Points,
                    FineAmount = v.FineAmount
                }).ToList(),
                PerformanceMetrics = performanceMetrics?.Metrics,
                RetrievedAt = DateTime.UtcNow
            } : null,
            ComplianceIssues = (complianceStatus?.Issues ?? new List<ComplianceIssue>())
                .Select(i => new ComplianceIssue
                {
                    Type = i.Type,
                    Description = i.Description,
                    Severity = i.Severity,
                    IdentifiedDate = i.IdentifiedDate,
                    DueDate = i.DueDate,
                    Resolved = i.Resolved,
                    ResolvedDate = i.ResolvedDate
                }).ToList()
        };
    }

    private void UpdateCarrierData(
        Carrier carrier,
        HighwayCarrierData? highwayData,
        CarrierSafetyRating? safetyRating,
        CarrierInsuranceInfo? insuranceInfo,
        CarrierOperatingAuthority? operatingAuthority,
        CarrierCompliance? complianceStatus,
        List<CarrierViolation> violations,
        CarrierAuditResult? auditResults,
        CarrierPerformanceMetrics? performanceMetrics)
    {
        // Update basic info if available
        if (highwayData != null)
        {
            carrier.McNumber = highwayData.McNumber ?? carrier.McNumber;
            carrier.LegalName = highwayData.LegalName ?? carrier.LegalName;
            carrier.DbaName = highwayData.DbaName ?? carrier.DbaName;
            carrier.Address = highwayData.Address ?? carrier.Address;
            carrier.City = highwayData.City ?? carrier.City;
            carrier.State = highwayData.State ?? carrier.State;
            carrier.ZipCode = highwayData.ZipCode ?? carrier.ZipCode;
            carrier.Phone = highwayData.Phone ?? carrier.Phone;
            carrier.Email = highwayData.Email ?? carrier.Email;
        }

        // Update Highway data
        carrier.HighwayData = highwayData != null ? new HighwayCarrierData
        {
            SafetyRating = safetyRating?.OverallRating ?? string.Empty,
            SafetyScore = safetyRating?.Score,
            InsuranceProvider = insuranceInfo?.InsuranceProvider,
            InsuranceExpiration = insuranceInfo?.ExpirationDate,
            OperatingAuthorities = operatingAuthority?.AuthorityTypes.Select(a => new OperatingAuthority
            {
                Type = a.Type,
                Description = a.Description,
                GrantedDate = a.GrantedDate,
                ExpirationDate = a.ExpirationDate
            }).ToList() ?? new(),
            RetrievedAt = DateTime.UtcNow
        } : null;

        // Update McLeod data
        carrier.McLeodData = complianceStatus != null ? new McLeodCarrierData
        {
            ComplianceStatus = complianceStatus.Status,
            ComplianceScore = complianceStatus.ComplianceScore,
            LastAuditDate = complianceStatus.LastAuditDate,
            ComplianceIssues = complianceStatus.Issues.Select(i => new ComplianceIssue
            {
                Type = i.Type,
                Description = i.Description,
                Severity = i.Severity,
                IdentifiedDate = i.IdentifiedDate,
                DueDate = i.DueDate,
                Resolved = i.Resolved,
                ResolvedDate = i.ResolvedDate
            }).ToList(),
            Violations = violations.Select(v => new PerformanceViolation
            {
                Type = v.Type,
                Description = v.Description,
                Date = v.Date,
                Points = v.Points,
                FineAmount = v.FineAmount
            }).ToList(),
            PerformanceMetrics = performanceMetrics?.Metrics,
            RetrievedAt = DateTime.UtcNow
        } : null;

        // Update compliance issues
        carrier.ComplianceIssues = (complianceStatus?.Issues ?? new List<ComplianceIssue>())
            .Select(i => new ComplianceIssue
            {
                Type = i.Type,
                Description = i.Description,
                Severity = i.Severity,
                IdentifiedDate = i.IdentifiedDate,
                DueDate = i.DueDate,
                Resolved = i.Resolved,
                ResolvedDate = i.ResolvedDate
            }).ToList();
    }
}

/// <summary>
/// Carrier vetting result
/// </summary>
public class CarrierVettingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Carrier? Carrier { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public double VettingScore { get; set; }
    public DateTime VettingDate { get; set; }
    public VettingStatus? PreviousVettingStatus { get; set; }
    public double? PreviousVettingScore { get; set; }
    public DateTime? PreviousVettingDate { get; set; }
    public HighwayCarrierData? HighwayData { get; set; }
    public CarrierSafetyRating? SafetyRating { get; set; }
    public CarrierInsuranceInfo? InsuranceInfo { get; set; }
    public CarrierCompliance? ComplianceStatus { get; set; }
    public List<CarrierViolation> Violations { get; set; } = new();
    public CarrierAuditResult? AuditResults { get; set; }
    public CarrierPerformanceMetrics? PerformanceMetrics { get; set; }
}
