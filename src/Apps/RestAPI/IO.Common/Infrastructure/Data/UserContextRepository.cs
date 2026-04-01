using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Infrastructure.Data;

/// <summary>
/// MongoDB repository for user contexts using USXpress.Configuration.Mongo
/// </summary>
public class UserContextRepository
{
    private readonly IMongoRepository<ContextData> _repository;
    private readonly ILogger<UserContextRepository> _logger;

    public UserContextRepository(
        IMongoRepository<ContextData> repository,
        ILogger<UserContextRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContextData?> GetByIdAsync(string id)
    {
        try
        {
            return await _repository.FindOneAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context with ID: {Id}", id);
            throw;
        }
    }

    public async Task<ContextData?> GetByUserIdAsync(string userId)
    {
        try
        {
            return await _repository.FindOneAsync(c => c.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context by user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<ContextData> CreateAsync(ContextData contextData)
    {
        try
        {
            contextData.CreatedAt = DateTime.UtcNow;
            contextData.UpdatedAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(contextData);
            return contextData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user context");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(ContextData contextData)
    {
        try
        {
            contextData.UpdatedAt = DateTime.UtcNow;
            
            await _repository.ReplaceOneAsync(c => c.Id == contextData.Id, contextData);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user context with ID: {Id}", contextData.Id);
            throw;
        }
    }
}
