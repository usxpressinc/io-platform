using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace IO.Cass.Infrastructure.Data;

/// <summary>
/// MongoDB repository for carrier data using USXpress.Configuration.Mongo
/// </summary>
public class CarrierRepository
{
    private readonly IMongoRepository<Carrier> _repository;
    private readonly ILogger<CarrierRepository> _logger;

    public CarrierRepository(
        IMongoRepository<Carrier> repository,
        ILogger<CarrierRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Carrier?> GetByDotNumberAsync(string dotNumber)
    {
        try
        {
            return await _repository.FindOneAsync(c => c.DotNumber == dotNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier by DOT number: {DotNumber}", dotNumber);
            throw;
        }
    }

    public async Task<Carrier?> GetByMcNumberAsync(string mcNumber)
    {
        try
        {
            return await _repository.FindOneAsync(c => c.McNumber == mcNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier by MC number: {McNumber}", mcNumber);
            throw;
        }
    }

    public async Task<List<Carrier>> GetByNameAsync(string name, int limit = 50)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c => c.LegalName.Contains(name));
            return carriers.OrderByDescending(c => c.UpdatedAt).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carriers by name: {Name}", name);
            throw;
        }
    }

    public async Task<List<Carrier>> GetByStatusAsync(CarrierStatus status, int limit = 100)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c => c.Status == status);
            return carriers.OrderByDescending(c => c.UpdatedAt).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carriers by status: {Status}", status);
            throw;
        }
    }

    public async Task<List<Carrier>> GetByStateAsync(string state, int limit = 100)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c => c.State == state);
            return carriers.OrderByDescending(c => c.UpdatedAt).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carriers by state: {State}", state);
            throw;
        }
    }

    public async Task<Carrier> CreateAsync(Carrier carrier)
    {
        try
        {
            carrier.CreatedAt = DateTime.UtcNow;
            carrier.UpdatedAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(carrier);
            return carrier;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating carrier");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Carrier carrier)
    {
        try
        {
            carrier.UpdatedAt = DateTime.UtcNow;
            
            await _repository.ReplaceOneAsync(c => c.Id == carrier.Id, carrier);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating carrier with ID: {Id}", carrier.Id);
            throw;
        }
    }

    public async Task<bool> UpdateVettingStatusAsync(string dotNumber, VettingStatus status, double score)
    {
        try
        {
            var update = new
            {
                VettingStatus = status,
                VettingScore = score,
                LastVettedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.FindOneAndUpdateAsync(
                c => c.DotNumber == dotNumber,
                update);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vetting status for DOT number: {DotNumber}", dotNumber);
            throw;
        }
    }

    public async Task<List<Carrier>> SearchAsync(CarrierSearchRequest request)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c =>
                (string.IsNullOrEmpty(request.Name) || c.LegalName.Contains(request.Name)) &&
                (string.IsNullOrEmpty(request.State) || c.State == request.State) &&
                (string.IsNullOrEmpty(request.DotNumber) || c.DotNumber == request.DotNumber) &&
                (string.IsNullOrEmpty(request.McNumber) || c.McNumber == request.McNumber) &&
                (!request.Status.HasValue || c.Status == request.Status.Value) &&
                (!request.VettingStatus.HasValue || c.VettingStatus == request.VettingStatus.Value));

            return carriers.OrderByDescending(c => c.UpdatedAt).Take(request.Limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching carriers");
            throw;
        }
    }

    public async Task<long> GetCountByStatusAsync(CarrierStatus status)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c => c.Status == status);
            return carriers.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier count by status: {Status}", status);
            throw;
        }
    }

    public async Task<long> GetCountByVettingStatusAsync(VettingStatus status)
    {
        try
        {
            var carriers = await _repository.FindManyAsync(c => c.VettingStatus == status);
            return carriers.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carrier count by vetting status: {Status}", status);
            throw;
        }
    }
}

/// <summary>
/// Carrier entity
/// </summary>
public class Carrier
{
    public string Id { get; set; } = string.Empty;
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
    public CarrierStatus Status { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public double VettingScore { get; set; }
    public DateTime? LastVettedAt { get; set; }
    public HighwayCarrierData? HighwayData { get; set; }
    public McLeodCarrierData? McLeodData { get; set; }
    public List<ComplianceIssue> ComplianceIssues { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Carrier status
/// </summary>
public enum CarrierStatus
{
    Active,
    Inactive,
    Suspended,
    Unknown
}

/// <summary>
/// Vetting status
/// </summary>
public enum VettingStatus
{
    NotVetted,
    InProgress,
    Approved,
    Rejected,
    RequiresReview,
    Unknown
}

/// <summary>
/// Highway carrier data
/// </summary>
public class HighwayCarrierData
{
    public string SafetyRating { get; set; } = string.Empty;
    public double? SafetyScore { get; set; }
    public string? InsuranceProvider { get; set; }
    public DateTime? InsuranceExpiration { get; set; }
    public List<OperatingAuthority> OperatingAuthorities { get; set; } = new();
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Operating authority
/// </summary>
public class OperatingAuthority
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? GrantedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// McLeod carrier data
/// </summary>
public class McLeodCarrierData
{
    public ComplianceStatus ComplianceStatus { get; set; }
    public double ComplianceScore { get; set; }
    public DateTime? LastAuditDate { get; set; }
    public List<ComplianceIssue> ComplianceIssues { get; set; } = new();
    public List<PerformanceViolation> Violations { get; set; } = new();
    public PerformanceMetrics? PerformanceMetrics { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Performance violation
/// </summary>
public class PerformanceViolation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Points { get; set; }
    public decimal FineAmount { get; set; }
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public double OnTimeDeliveryPercentage { get; set; }
    public double CustomerSatisfactionScore { get; set; }
    public int TotalLoads { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Carrier search request
/// </summary>
public class CarrierSearchRequest
{
    public string? Name { get; set; }
    public string? State { get; set; }
    public string? DotNumber { get; set; }
    public string? McNumber { get; set; }
    public CarrierStatus? Status { get; set; }
    public VettingStatus? VettingStatus { get; set; }
    public int Limit { get; set; } = 100;
}
