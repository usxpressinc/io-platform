using Microsoft.Extensions.Logging;
using IO.Lea.Infrastructure.GoogleJobs;
using IO.Lea.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IO.Lea.Core.Jobs;

/// <summary>
/// Job processing business logic service
/// </summary>
public interface IJobBusinessService
{
    Task<Job> CreateJobAsync(Job job);
    Task<Job?> GetJobByIdAsync(string id);
    Task<List<Job>> GetJobsByStatusAsync(JobStatus status, int limit = 100);
    Task<List<Job>> GetJobsByCustomerIdAsync(string customerId, int limit = 100);
    Task<bool> UpdateJobStatusAsync(string id, JobStatus status);
    Task<bool> ProcessJobAsync(string id);
    Task<GoogleJobPosting> PostToGoogleJobsAsync(string jobId);
    Task<bool> UpdateGoogleJobStatusAsync(string jobId, GoogleJobStatus status);
    Task<List<Job>> SearchJobsAsync(JobSearchRequest request);
    Task<List<Job>> LookupJobsByEndpointAsync(string endpoint);
}

/// <summary>
/// Job service implementation
/// </summary>
public class JobBusinessService : IJobBusinessService
{
    private readonly JobRepository _jobRepository;
    private readonly GoogleJobsApiClient _googleJobsClient;
    private readonly ILogger<JobBusinessService> _logger;

    public JobBusinessService(
        JobRepository jobRepository,
        GoogleJobsApiClient googleJobsClient,
        ILogger<JobBusinessService> logger)
    {
        _jobRepository = jobRepository;
        _googleJobsClient = googleJobsClient;
        _logger = logger;
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        try
        {
            _logger.LogInformation("Creating job {Title} for customer {CustomerId}", job.Title, job.CustomerId);

            // Validate job
            ValidateJob(job, forCreate: true);

            // Set default values
            job.Status = JobStatus.Created;
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            return await _jobRepository.CreateAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job {Title}", job.Title);
            throw;
        }
    }

    public async Task<Job?> GetJobByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Getting job {Id}", id);
            return await _jobRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {Id}", id);
            return null;
        }
    }

    public async Task<List<Job>> GetJobsByStatusAsync(JobStatus status, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting jobs with status {Status}", status);
            return await _jobRepository.GetByStatusAsync(status, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by status {Status}", status);
            return new List<Job>();
        }
    }

    public async Task<List<Job>> GetJobsByCustomerIdAsync(string customerId, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting jobs for customer {CustomerId}", customerId);
            return await _jobRepository.GetByCustomerIdAsync(customerId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for customer {CustomerId}", customerId);
            return new List<Job>();
        }
    }

    public async Task<bool> UpdateJobStatusAsync(string id, JobStatus status)
    {
        try
        {
            _logger.LogInformation("Updating job {Id} status to {Status}", id, status);
            
            var job = await _jobRepository.GetByIdAsync(id);
            if (job == null)
            {
                return false;
            }

            job.Status = status;
            job.UpdatedAt = DateTime.UtcNow;

            // Post to Google Jobs if status is Published
            if (status == JobStatus.Published && job.GoogleJobId == null)
            {
                var googleJob = await PostToGoogleJobsAsync(id);
                if (googleJob != null)
                {
                    job.GoogleJobId = googleJob.Id;
                    job.GoogleJobStatus = GoogleJobStatus.Active;
                }
            }

            return await _jobRepository.UpdateAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job {Id} status", id);
            return false;
        }
    }

    public async Task<bool> ProcessJobAsync(string id)
    {
        try
        {
            _logger.LogInformation("Processing job {Id}", id);

            var job = await _jobRepository.GetByIdAsync(id);
            if (job == null)
            {
                return false;
            }

            // Update status to Processing
            job.Status = JobStatus.Processing;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);

            // Simulate job processing (in real implementation, this would do actual work)
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Update status to Completed
            job.Status = JobStatus.Completed;
            job.UpdatedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;

            return await _jobRepository.UpdateAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {Id}", id);
            return false;
        }
    }

    public async Task<GoogleJobPosting> PostToGoogleJobsAsync(string jobId)
    {
        try
        {
            _logger.LogInformation("Posting job {JobId} to Google Jobs", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new NotFoundException("Job", jobId);
            }

            var googleJobRequest = new GoogleJobRequest
            {
                Title = job.Title,
                Description = job.Description,
                Location = job.Location,
                EmploymentType = job.EmploymentType,
                SalaryRange = job.SalaryRange,
                CompanyName = job.CompanyName,
                ApplicationUrl = job.ApplicationUrl,
                PostedAt = job.CreatedAt
            };

            var googleJob = await _googleJobsClient.PostJobAsync(googleJobRequest);

            if (googleJob != null)
            {
                _logger.LogInformation("Successfully posted job {JobId} to Google Jobs with Google ID {GoogleJobId}", 
                    jobId, googleJob.Id);
            }

            return googleJob;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting job {JobId} to Google Jobs", jobId);
            throw;
        }
    }

    public async Task<bool> UpdateGoogleJobStatusAsync(string jobId, GoogleJobStatus status)
    {
        try
        {
            _logger.LogInformation("Updating Google Job status for job {JobId} to {Status}", jobId, status);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null || job.GoogleJobId == null)
            {
                return false;
            }

            var success = await _googleJobsClient.UpdateJobStatusAsync(job.GoogleJobId, status);

            if (success)
            {
                job.GoogleJobStatus = status;
                job.UpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Google Job status for job {JobId}", jobId);
            return false;
        }
    }

    public async Task<List<Job>> SearchJobsAsync(JobSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching jobs with criteria: {Criteria}", request);
            return await _jobRepository.SearchAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching jobs");
            return new List<Job>();
        }
    }

    public async Task<List<Job>> LookupJobsByEndpointAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation("Looking up jobs for endpoint: {Endpoint}", endpoint);
            
            // For now, return jobs by searching title/description for endpoint reference
            // In a real implementation, this would use a more sophisticated matching algorithm
            var searchRequest = new JobSearchRequest
            {
                Query = endpoint,
                Limit = 50
            };
            
            return await _jobRepository.SearchAsync(searchRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up jobs for endpoint {Endpoint}", endpoint);
            return new List<Job>();
        }
    }

    private void ValidateJob(Job job, bool forCreate)
    {
        if (job == null)
        {
            throw new ValidationException("Job is required");
        }

        if (forCreate && string.IsNullOrEmpty(job.CustomerId))
        {
            throw new ValidationException("Customer ID is required for creation");
        }

        if (string.IsNullOrEmpty(job.Title))
        {
            throw new ValidationException("Job title is required");
        }

        if (string.IsNullOrEmpty(job.Description))
        {
            throw new ValidationException("Job description is required");
        }

        if (string.IsNullOrEmpty(job.Location))
        {
            throw new ValidationException("Job location is required");
        }

        if (string.IsNullOrEmpty(job.EmploymentType))
        {
            throw new ValidationException("Employment type is required");
        }

        if (string.IsNullOrEmpty(job.CompanyName))
        {
            throw new ValidationException("Company name is required");
        }

        if (string.IsNullOrEmpty(job.ApplicationUrl))
        {
            throw new ValidationException("Application URL is required");
        }
    }
}

/// <summary>
/// Job search request
/// </summary>
public class JobSearchRequest
{
    public string? Query { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? CompanyName { get; set; }
    public JobStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Job entity
/// </summary>
public class Job
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public string? GoogleJobId { get; set; }
    public GoogleJobStatus? GoogleJobStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Job status enumeration
/// </summary>
public enum JobStatus
{
    Created,
    Processing,
    Completed,
    Published,
    Expired,
    Cancelled,
    Error
}

/// <summary>
/// Google Job status enumeration
/// </summary>
public enum GoogleJobStatus
{
    Active,
    Inactive,
    Expired,
    Deleted,
    Error
}
