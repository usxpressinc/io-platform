using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Lea.Infrastructure.Data;

/// <summary>
/// MongoDB repository for job listings using USXpress.Configuration.Mongo
/// </summary>
public class JobRepository
{
    private readonly IMongoRepository<JobListing> _repository;
    private readonly ILogger<JobRepository> _logger;

    public JobRepository(
        IMongoRepository<JobListing> repository,
        ILogger<JobRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<JobListing?> GetByIdAsync(string id)
    {
        try
        {
            return await _repository.FindOneAsync(j => j.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job with ID: {Id}", id);
            throw;
        }
    }

    public async Task<List<JobListing>> GetByLocationAsync(string location)
    {
        try
        {
            return await _repository.FindManyAsync(j => j.Location == location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by location: {Location}", location);
            throw;
        }
    }

    public async Task<JobListing> CreateAsync(JobListing job)
    {
        try
        {
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(job);
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(JobListing job)
    {
        try
        {
            job.UpdatedAt = DateTime.UtcNow;
            
            await _repository.ReplaceOneAsync(j => j.Id == job.Id, job);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job with ID: {Id}", job.Id);
            throw;
        }
    }
}
