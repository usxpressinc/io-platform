using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Larry.Infrastructure.Data;

/// <summary>
/// MongoDB repository for vendors using USXpress.Configuration.Mongo
/// </summary>
public class VendorRepository
{
    private readonly IMongoRepository<Vendor> _repository;
    private readonly ILogger<VendorRepository> _logger;

    public VendorRepository(
        IMongoRepository<Vendor> repository,
        ILogger<VendorRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Vendor?> GetByIdAsync(string id)
    {
        try
        {
            return await _repository.FindOneAsync(v => v.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor with ID: {Id}", id);
            throw;
        }
    }

    public async Task<Vendor?> GetByVendorCodeAsync(string vendorCode)
    {
        try
        {
            return await _repository.FindOneAsync(v => v.VendorCode == vendorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor by code: {VendorCode}", vendorCode);
            throw;
        }
    }

    public async Task<Vendor> CreateAsync(Vendor vendor)
    {
        try
        {
            vendor.CreatedAt = DateTime.UtcNow;
            vendor.UpdatedAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(vendor);
            return vendor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Vendor vendor)
    {
        try
        {
            vendor.UpdatedAt = DateTime.UtcNow;
            
            await _repository.ReplaceOneAsync(v => v.Id == vendor.Id, vendor);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor with ID: {Id}", vendor.Id);
            throw;
        }
    }
}
