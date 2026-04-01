using Microsoft.Extensions.Logging;
using IO.Larry.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Larry.Core.Vendors;

/// <summary>
/// Vendor management business logic service
/// </summary>
public interface IVendorBusinessService
{
    Task<Vendor> CreateVendorAsync(Vendor vendor);
    Task<Vendor?> GetVendorByIdAsync(string id);
    Task<Vendor?> GetVendorByCodeAsync(string code);
    Task<List<Vendor>> GetVendorsByStatusAsync(VendorStatus status, int limit = 100);
    Task<List<Vendor>> SearchVendorsAsync(VendorSearchRequest request);
    Task<bool> UpdateVendorAsync(Vendor vendor);
    Task<bool> UpdateVendorStatusAsync(string id, VendorStatus status);
    Task<List<VendorPerformance>> GetVendorPerformanceAsync(string vendorId);
    Task<VendorCompliance> GetVendorComplianceAsync(string vendorId);
    Task<List<Vendor>> LookupVendorsByLocationAsync(string location);
}

/// <summary>
/// Vendor service implementation
/// </summary>
public class VendorBusinessService : IVendorBusinessService
{
    private readonly VendorRepository _vendorRepository;
    private readonly VendorPerformanceRepository _performanceRepository;
    private readonly VendorComplianceRepository _complianceRepository;
    private readonly ILogger<VendorBusinessService> _logger;

    public VendorBusinessService(
        VendorRepository vendorRepository,
        VendorPerformanceRepository performanceRepository,
        VendorComplianceRepository complianceRepository,
        ILogger<VendorBusinessService> logger)
    {
        _vendorRepository = vendorRepository;
        _performanceRepository = performanceRepository;
        _complianceRepository = complianceRepository;
        _logger = logger;
    }

    public async Task<Vendor> CreateVendorAsync(Vendor vendor)
    {
        try
        {
            _logger.LogInformation("Creating vendor {VendorCode}", vendor.VendorCode);

            // Validate vendor
            ValidateVendor(vendor, forCreate: true);

            // Check if vendor code already exists
            var existingVendor = await _vendorRepository.GetByCodeAsync(vendor.VendorCode);
            if (existingVendor != null)
            {
                throw new ConflictException("VendorCode", vendor.VendorCode, "Vendor code already exists");
            }

            // Set default values
            vendor.Status = VendorStatus.Active;
            vendor.CreatedAt = DateTime.UtcNow;
            vendor.UpdatedAt = DateTime.UtcNow;

            return await _vendorRepository.CreateAsync(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor {VendorCode}", vendor.VendorCode);
            throw;
        }
    }

    public async Task<Vendor?> GetVendorByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Getting vendor {Id}", id);
            return await _vendorRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor {Id}", id);
            return null;
        }
    }

    public async Task<Vendor?> GetVendorByCodeAsync(string code)
    {
        try
        {
            _logger.LogDebug("Getting vendor by code {Code}", code);
            return await _vendorRepository.GetByCodeAsync(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor by code {Code}", code);
            return null;
        }
    }

    public async Task<List<Vendor>> GetVendorsByStatusAsync(VendorStatus status, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting vendors with status {Status}", status);
            return await _vendorRepository.GetByStatusAsync(status, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendors by status {Status}", status);
            return new List<Vendor>();
        }
    }

    public async Task<List<Vendor>> SearchVendorsAsync(VendorSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching vendors with criteria: {Criteria}", request);
            return await _vendorRepository.SearchAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vendors");
            return new List<Vendor>();
        }
    }

    public async Task<bool> UpdateVendorAsync(Vendor vendor)
    {
        try
        {
            _logger.LogInformation("Updating vendor {Id}", vendor.Id);

            // Validate vendor
            ValidateVendor(vendor, forCreate: false);

            vendor.UpdatedAt = DateTime.UtcNow;
            return await _vendorRepository.UpdateAsync(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {Id}", vendor.Id);
            return false;
        }
    }

    public async Task<bool> UpdateVendorStatusAsync(string id, VendorStatus status)
    {
        try
        {
            _logger.LogInformation("Updating vendor {Id} status to {Status}", id, status);
            return await _vendorRepository.UpdateStatusAsync(id, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {Id} status", id);
            return false;
        }
    }

    public async Task<List<VendorPerformance>> GetVendorPerformanceAsync(string vendorId)
    {
        try
        {
            _logger.LogInformation("Getting performance metrics for vendor {VendorId}", vendorId);
            return await _performanceRepository.GetByVendorIdAsync(vendorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor performance {VendorId}", vendorId);
            return new List<VendorPerformance>();
        }
    }

    public async Task<VendorCompliance> GetVendorComplianceAsync(string vendorId)
    {
        try
        {
            _logger.LogInformation("Getting compliance status for vendor {VendorId}", vendorId);
            
            var compliance = await _complianceRepository.GetByVendorIdAsync(vendorId);
            if (compliance == null)
            {
                // Create default compliance record
                compliance = new VendorCompliance
                {
                    VendorId = vendorId,
                    OverallStatus = ComplianceStatus.Unknown,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                compliance = await _complianceRepository.CreateAsync(compliance);
            }

            return compliance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor compliance {VendorId}", vendorId);
            return new VendorCompliance
            {
                VendorId = vendorId,
                OverallStatus = ComplianceStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<Vendor>> LookupVendorsByLocationAsync(string location)
    {
        try
        {
            _logger.LogInformation("Looking up vendors for location: {Location}", location);
            
            // For now, return vendors by searching city/state
            // In a real implementation, this would use geolocation services
            var searchRequest = new VendorSearchRequest
            {
                City = location,
                Limit = 50
            };
            
            return await _vendorRepository.SearchAsync(searchRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up vendors for location {Location}", location);
            return new List<Vendor>();
        }
    }

    private void ValidateVendor(Vendor vendor, bool forCreate)
    {
        if (vendor == null)
        {
            throw new ValidationException("Vendor is required");
        }

        if (forCreate && string.IsNullOrEmpty(vendor.VendorCode))
        {
            throw new ValidationException("Vendor code is required for creation");
        }

        if (string.IsNullOrEmpty(vendor.Name))
        {
            throw new ValidationException("Vendor name is required");
        }

        if (string.IsNullOrEmpty(vendor.ContactEmail))
        {
            throw new ValidationException("Contact email is required");
        }

        if (!IsValidEmail(vendor.ContactEmail))
        {
            throw new ValidationException($"Invalid email format: {vendor.ContactEmail}");
        }

        if (vendor.Services?.Any() != true)
        {
            throw new ValidationException("At least one service is required");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Vendor search request
/// </summary>
public class VendorSearchRequest
{
    public string? Name { get; set; }
    public string? Service { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public VendorStatus? Status { get; set; }
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Vendor entity
/// </summary>
public class Vendor
{
    public string Id { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public List<string> Services { get; set; } = new();
    public VendorStatus Status { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Vendor status enumeration
/// </summary>
public enum VendorStatus
{
    Active,
    Inactive,
    Suspended,
    UnderReview,
    Unknown
}

/// <summary>
/// Vendor performance metrics
/// </summary>
public class VendorPerformance
{
    public string Id { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int DelayedDeliveries { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double CustomerSatisfactionScore { get; set; }
    public double QualityScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Vendor compliance information
/// </summary>
public class VendorCompliance
{
    public string Id { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public ComplianceStatus OverallStatus { get; set; }
    public double ComplianceScore { get; set; }
    public DateTime? LastAuditDate { get; set; }
    public DateTime? NextAuditDate { get; set; }
    public List<ComplianceIssue> Issues { get; set; } = new();
    public List<ComplianceRequirement> Requirements { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Compliance status enumeration
/// </summary>
public enum ComplianceStatus
{
    Compliant,
    NonCompliant,
    Pending,
    Unknown,
    Error
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
/// Issue severity enumeration
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
