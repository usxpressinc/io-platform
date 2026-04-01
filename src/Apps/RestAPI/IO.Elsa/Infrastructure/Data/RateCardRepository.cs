using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Elsa.Infrastructure.Data;

/// <summary>
/// MongoDB repository for rate cards using USXpress.Configuration.Mongo
/// </summary>
public class RateCardRepository
{
    private readonly IMongoRepository<RateCard> _repository;
    private readonly ILogger<RateCardRepository> _logger;

    public RateCardRepository(
        IMongoRepository<RateCard> repository,
        ILogger<RateCardRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RateCard?> GetByIdAsync(string id)
    {
        try
        {
            return await _repository.FindOneAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate card with ID: {Id}", id);
            throw;
        }
    }

    public async Task<RateCard> CreateAsync(RateCard rateCard)
    {
        try
        {
            rateCard.CreatedAt = DateTime.UtcNow;
            rateCard.UpdatedAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(rateCard);
            return rateCard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rate card");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(RateCard rateCard)
    {
        try
        {
            rateCard.UpdatedAt = DateTime.UtcNow;
            
            await _repository.ReplaceOneAsync(r => r.Id == rateCard.Id, rateCard);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate card with ID: {Id}", rateCard.Id);
            throw;
        }
    }
}
