using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IO.Lea.Core.Jobs;
using IO.Lea.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Lea.Endpoints;

/// <summary>
/// Job endpoints for IO.Lea service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly IJobBusinessService _jobService;
    private readonly ILogger<JobController> _logger;

    public JobController(
        IJobBusinessService jobService,
        ILogger<JobController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new job
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] Job job)
    {
        try
        {
            if (job == null)
            {
                return BadRequest(new { error = "Job data is required" });
            }

            _logger.LogInformation("Creating job {Title} for customer {CustomerId}", job.Title, job.CustomerId);

            var createdJob = await _jobService.CreateJobAsync(job);

            return CreatedAtAction(
                nameof(GetJob),
                new { id = createdJob.Id },
                createdJob);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating job");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get job by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Job ID is required" });
            }

            _logger.LogInformation("Getting job {Id}", id);

            var job = await _jobService.GetJobByIdAsync(id);

            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update job status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateJobStatus(string id, [FromBody] UpdateJobStatusRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Job ID is required" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Status update request is required" });
            }

            _logger.LogInformation("Updating job {Id} status to {Status}", id, request.Status);

            var success = await _jobService.UpdateJobStatusAsync(id, request.Status);

            if (success)
            {
                return Ok(new { message = "Job status updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update job status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {Id} status", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process a job
    /// </summary>
    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessJob(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Job ID is required" });
            }

            _logger.LogInformation("Processing job {Id}", id);

            var success = await _jobService.ProcessJobAsync(id);

            if (success)
            {
                return Ok(new { message = "Job processed successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to process job" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Post job to Google Jobs
    /// </summary>
    [HttpPost("{id}/google-jobs")]
    public async Task<IActionResult> PostToGoogleJobs(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Job ID is required" });
            }

            _logger.LogInformation("Posting job {Id} to Google Jobs", id);

            var googleJob = await _jobService.PostToGoogleJobsAsync(id);

            if (googleJob != null)
            {
                return Ok(new { 
                    message = "Job posted to Google Jobs successfully",
                    googleJob
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to post job to Google Jobs" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting job {Id} to Google Jobs", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update Google Job status
    /// </summary>
    [HttpPut("{id}/google-jobs/status")]
    public async Task<IActionResult> UpdateGoogleJobStatus(string id, [FromBody] UpdateGoogleJobStatusRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Job ID is required" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Google Job status update request is required" });
            }

            _logger.LogInformation("Updating Google Job status for job {Id} to {Status}", id, request.Status);

            var success = await _jobService.UpdateGoogleJobStatusAsync(id, request.Status);

            if (success)
            {
                return Ok(new { message = "Google Job status updated successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update Google Job status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Google Job status for job {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get jobs by status
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetJobsByStatus(JobStatus status, [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting jobs with status {Status}", status);

            var jobs = await _jobService.GetJobsByStatusAsync(status, limit);

            return Ok(new { 
                jobs,
                count = jobs.Count,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by status {Status}", status);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get jobs by customer
    /// </summary>
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetJobsByCustomer(string customerId, [FromQuery] int limit = 100)
    {
        try
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest(new { error = "Customer ID is required" });
            }

            _logger.LogInformation("Getting jobs for customer {CustomerId}", customerId);

            var jobs = await _jobService.GetJobsByCustomerIdAsync(customerId, limit);

            return Ok(new { 
                jobs,
                count = jobs.Count,
                customerId,
                limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for customer {CustomerId}", customerId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search jobs
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchJobs([FromBody] JobSearchRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Search request is required" });
            }

            _logger.LogInformation("Searching jobs with criteria: {Criteria}", request);

            var jobs = await _jobService.SearchJobsAsync(request);

            return Ok(new { 
                jobs,
                count = jobs.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching jobs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get job statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetJobStatistics()
    {
        try
        {
            _logger.LogInformation("Getting job statistics");

            // This would typically use repository methods to get aggregated statistics
            // For now, return placeholder data
            var statistics = new
            {
                totalJobs = 0,
                createdJobs = 0,
                processingJobs = 0,
                completedJobs = 0,
                publishedJobs = 0,
                expiredJobs = 0,
                googleJobsPosted = 0,
                averageProcessingTime = TimeSpan.Zero,
                lastUpdated = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get list of jobs for an endpoint (matches Python functionality)
    /// </summary>
    [HttpPost("lookup")]
    public async Task<IActionResult> LookupJobs([FromBody] JobLookupRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Job lookup request is required" });
            }

            _logger.LogInformation("Looking up jobs for endpoint: {Endpoint}", request.Endpoint);

            var jobs = await _jobService.LookupJobsByEndpointAsync(request.Endpoint);

            return Ok(new
            {
                success = true,
                data = jobs,
                count = jobs.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job lookup");
            return StatusCode(500, new
            {
                success = false,
                error = "Internal server error"
            });
        }
    }
}

/// <summary>
/// Update job status request
/// </summary>
public class UpdateJobStatusRequest
{
    public JobStatus Status { get; set; }
}

/// <summary>
/// Update Google Job status request
/// </summary>
public class UpdateGoogleJobStatusRequest
{
    public GoogleJobStatus Status { get; set; }
}

/// <summary>
/// Job lookup request (matches Python functionality)
/// </summary>
public class JobLookupRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public int? Limit { get; set; }
}
