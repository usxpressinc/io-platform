using Microsoft.Extensions.Logging;
using USXpress.Configuration.Mongo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Infrastructure.Data;

/// <summary>
/// MongoDB repository for email logs using USXpress.Configuration.Mongo
/// </summary>
public class EmailLogRepository
{
    private readonly IMongoRepository<EmailLog> _repository;
    private readonly ILogger<EmailLogRepository> _logger;

    public EmailLogRepository(
        IMongoRepository<EmailLog> repository,
        ILogger<EmailLogRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<EmailLog?> GetByIdAsync(string id)
    {
        try
        {
            return await _repository.FindOneAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email log with ID: {Id}", id);
            throw;
        }
    }

    public async Task<List<EmailLog>> GetByMessageIdAsync(string messageId)
    {
        try
        {
            return await _repository.FindManyAsync(e => e.MessageId == messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email logs by message ID: {MessageId}", messageId);
            throw;
        }
    }

    public async Task<EmailLog> CreateAsync(EmailLog emailLog)
    {
        try
        {
            emailLog.SentAt = DateTime.UtcNow;
            
            await _repository.InsertOneAsync(emailLog);
            return emailLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email log");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(EmailLog emailLog)
    {
        try
        {
            await _repository.ReplaceOneAsync(e => e.Id == emailLog.Id, emailLog);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email log with ID: {Id}", emailLog.Id);
            throw;
        }
    }
}
