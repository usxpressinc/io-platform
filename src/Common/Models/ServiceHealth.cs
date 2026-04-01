using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace IO.Platform.Common.Models;

/// <summary>
/// Represents service health status across all IO Platform microservices
/// Used for monitoring, health checks, and service discovery
/// </summary>
[BsonIgnoreExtraElements]
public class ServiceHealth
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("service_name")]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [BsonElement("service_type")]
    public ServiceType ServiceType { get; set; }

    [Required]
    [BsonElement("status")]
    public HealthStatus Status { get; set; }

    [BsonElement("version")]
    public string Version { get; set; } = "1.0.0";

    [BsonElement("host")]
    public string Host { get; set; } = string.Empty;

    [BsonElement("port")]
    public int Port { get; set; }

    [BsonElement("last_check")]
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;

    [BsonElement("uptime")]
    public TimeSpan Uptime { get; set; }

    [BsonElement("dependencies")]
    public List<DependencyHealth> Dependencies { get; set; } = new();

    [BsonElement("metrics")]
    public ServiceMetrics Metrics { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of services in the IO Platform
/// </summary>
public enum ServiceType
{
    ApiGateway,
    CommonService,
    CarrierService,
    PricingService,
    VendorService,
    JobService,
    Infrastructure
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Dependency health information
/// </summary>
[BsonIgnoreExtraElements]
public class DependencyHealth
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public DependencyType Type { get; set; }

    [BsonElement("status")]
    public HealthStatus Status { get; set; }

    [BsonElement("response_time_ms")]
    public int ResponseTimeMs { get; set; }

    [BsonElement("last_check")]
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;

    [BsonElement("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Types of dependencies
/// </summary>
public enum DependencyType
{
    Database,
    MessageQueue,
    HttpService,
    Cache,
    FileSystem
}

/// <summary>
/// Service metrics for monitoring
/// </summary>
[BsonIgnoreExtraElements]
public class ServiceMetrics
{
    [BsonElement("requests_per_second")]
    public double RequestsPerSecond { get; set; }

    [BsonElement("average_response_time_ms")]
    public double AverageResponseTimeMs { get; set; }

    [BsonElement("error_rate")]
    public double ErrorRate { get; set; }

    [BsonElement("cpu_usage_percent")]
    public double CpuUsagePercent { get; set; }

    [BsonElement("memory_usage_mb")]
    public double MemoryUsageMb { get; set; }

    [BsonElement("active_connections")]
    public int ActiveConnections { get; set; }

    [BsonElement("queue_depth")]
    public int QueueDepth { get; set; }

    [BsonElement("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Service health update request
/// </summary>
public class ServiceHealthUpdate
{
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    public HealthStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public List<DependencyHealth> Dependencies { get; set; } = new();

    public ServiceMetrics? Metrics { get; set; }
}
