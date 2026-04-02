using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IO.Common.Endpoints;

/// <summary>
/// Health check endpoints
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        this._healthCheckService = healthCheckService;
        this._logger = logger;
    }

    /// <summary>
    /// Perform a Health Check
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            this._logger.LogDebug("Health check requested");

            var healthReport = await this._healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    data = entry.Value.Data,
                    exception = entry.Value.Exception?.Message
                })
            };

            return healthReport.Status == HealthStatus.Healthy 
                ? this.Ok(response)
                : this.StatusCode(503, response); // Service Unavailable for unhealthy
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during health check");
            return this.StatusCode(500, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Health check failed"
            });
        }
    }

    /// <summary>
    /// Simple health check (matches Python functionality)
    /// </summary>
    [HttpGet("simple")]
    public IActionResult GetSimpleHealth()
    {
        try
        {
            this._logger.LogDebug("Simple health check requested");

            return this.Ok(new { status = "OK" });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during simple health check");
            return this.StatusCode(500, new { status = "ERROR" });
        }
    }
}
