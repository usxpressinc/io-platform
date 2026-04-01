using Microsoft.Extensions.Logging;
using IO.Common.Infrastructure.Data;
using IO.Common.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Common.Core;

/// <summary>
/// User context business logic service
/// </summary>
public interface IContextService
{
    Task<ContextResponse> GetContextAsync(string? id = null, string? number = null);
    Task<GenesysDriverResponse> GetGenesysDriverContextAsync(string? id = null, string? number = null);
    Task<bool> AddContextAsync(ContextRequest request);
}

/// <summary>
/// Context service implementation
/// </summary>
public class ContextService : IContextService
{
    private readonly UserContextRepository _userContextRepository;
    private readonly ILogger<ContextService> _logger;

    public ContextService(
        UserContextRepository userContextRepository,
        ILogger<ContextService> logger)
    {
        _userContextRepository = userContextRepository;
        _logger = logger;
    }

    public async Task<ContextResponse> GetContextAsync(string? id = null, string? number = null)
    {
        try
        {
            _logger.LogInformation("Getting user context for ID: {Id}, Number: {Number}", id, number);

            // For now, return a mock response - in production this would query actual systems
            var driver = new Driver
            {
                Id = id,
                Name = "Mock Driver",
                Sbu = "USX",
                Type = "Company Driver",
                Status = "Active",
                JobDesc = "Long Haul",
                Company = "USXpress",
                StateZone = "Central",
                PrimaryCoverage = "Full",
                PreferredLanguage = "English"
            };

            return new ContextResponse
            {
                Driver = driver
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context");
            return new ContextResponse
            {
                Driver = null
            };
        }
    }

    public async Task<GenesysDriverResponse> GetGenesysDriverContextAsync(string? id = null, string? number = null)
    {
        try
        {
            _logger.LogInformation("Getting Genesys driver context for ID: {Id}, Number: {Number}", id, number);

            // For now, return a mock response - in production this would integrate with Genesys
            var driver = new Dictionary<string, object>
            {
                ["driverID"] = id,
                ["driverName"] = "Mock Driver",
                ["driverCompany"] = "USXpress",
                ["driverSBU"] = "USX",
                ["driverType"] = "Company Driver",
                ["driverStatus"] = "Active",
                ["fleetManager"] = "Mock Manager",
                ["fleetServiceCenter"] = "Central",
                ["preferredLanguage"] = "English"
            };

            var call = new Dictionary<string, object>
            {
                ["callId"] = "mock-call-id",
                ["callStatus"] = "active"
            };

            return new GenesysDriverResponse
            {
                Driver = driver,
                Call = call
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Genesys driver context");
            return new GenesysDriverResponse
            {
                Driver = null,
                Call = null
            };
        }
    }

    public async Task<bool> AddContextAsync(ContextRequest request)
    {
        try
        {
            _logger.LogInformation("Adding user context for ID: {Id}, Number: {Number}", request.Id, request.Number);

            // In production, this would store the context in MongoDB
            // For now, just return success
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user context");
            return false;
        }
    }
}
