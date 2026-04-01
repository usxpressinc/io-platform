using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IO.Platform.Common.Core.Constants;
using USXpress.Configuration.Mongo;

namespace IO.Platform.Common.Infrastructure.MongoDb;

/// <summary>
/// MongoDB configuration using USXpress.Configuration.Mongo package
/// Follows USXpress patterns for MongoDB connectivity
/// </summary>
public static class MongoDbConfiguration
{
    /// <summary>
    /// Configure MongoDB services using USXpress.Configuration.Mongo
    /// </summary>
    public static IHostApplicationBuilder AddMongoDb(
        this IHostApplicationBuilder builder)
    {
        // Use USXpress MongoDB configuration patterns
        builder.UseDefaultMongoConventions();
        
        // Add repositories based on service needs
        // Each service will add their specific repositories
        return builder;
    }

    /// <summary>
    /// Add email logs repository (for IO.Common service)
    /// </summary>
    public static IHostApplicationBuilder AddEmailLogsRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.EmailLogs,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<EmailLog>(config);
        return builder;
    }

    /// <summary>
    /// Add context data repository (for IO.Common service)
    /// </summary>
    public static IHostApplicationBuilder AddContextDataRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.ContextData,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<ContextData>(config);
        return builder;
    }

    /// <summary>
    /// Add carrier data repository (for IO.Cass service)
    /// </summary>
    public static IHostApplicationBuilder AddCarrierDataRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.CarrierData,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<Carrier>(config);
        return builder;
    }

    /// <summary>
    /// Add pricing data repository (for IO.Elsa service)
    /// </summary>
    public static IHostApplicationBuilder AddPricingDataRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.PricingData,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<RateCard>(config);
        return builder;
    }

    /// <summary>
    /// Add vendor data repository (for IO.Larry service)
    /// </summary>
    public static IHostApplicationBuilder AddVendorDataRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.VendorData,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<Vendor>(config);
        return builder;
    }

    /// <summary>
    /// Add job listings repository (for IO.Lea service)
    /// </summary>
    public static IHostApplicationBuilder AddJobListingsRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoDbConnectionString]!,
            DatabaseName = builder.Configuration[EnvironmentVariables.MongoDbDatabaseName]!,
            CollectionName = MongoDbCollections.JobListings,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoDbTlsCertFile]
        };

        builder.Services.AddMongoRepository<JobListing>(config);
        return builder;
    }
}

/// <summary>
/// Email log entity for MongoDB
/// </summary>
public class EmailLog
{
    public string Id { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context data entity for MongoDB
/// </summary>
public class ContextData
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Carrier entity for MongoDB
/// </summary>
public class Carrier
{
    public string Id { get; set; } = string.Empty;
    public string DotNumber { get; set; } = string.Empty;
    public string McNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DbaName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Rate card entity for MongoDB
/// </summary>
public class RateCard
{
    public string Id { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string EquipmentType { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Vendor entity for MongoDB
/// </summary>
public class Vendor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Job listing entity for MongoDB
/// </summary>
public class JobListing
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime PostedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
