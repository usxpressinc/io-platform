using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IO.Platform.Common.Core.Configuration;

/// <summary>
/// Centralized environment configuration management for all IO Platform services
/// Provides consistent configuration patterns and validation
/// </summary>
public static class EnvironmentConfiguration
{
    /// <summary>
    /// Configure environment-specific settings with validation
    /// </summary>
    public static IServiceCollection AddEnvironmentConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        // Add configuration options with validation
        services.Configure<MongoDbConfiguration>(configuration.GetSection("MongoDB"));
        services.Configure<KafkaConfiguration>(configuration.GetSection("Kafka"));
        services.Configure<ServiceConfiguration>(configuration.GetSection("Service"));
        services.Configure<AuthenticationConfiguration>(configuration.GetSection("Authentication"));

        // Validate required settings
        ValidateConfiguration(configuration, serviceName);

        return services;
    }

    /// <summary>
    /// Validate that all required configuration settings are present
    /// </summary>
    private static void ValidateConfiguration(IConfiguration configuration, string serviceName)
    {
        var requiredSettings = new[]
        {
            $"MongoDB:ConnectionString",
            $"MongoDB:DatabaseName",
            $"Kafka:BootstrapServers",
            $"Authentication:MasterToken"
        };

        foreach (var setting in requiredSettings)
        {
            if (string.IsNullOrEmpty(configuration[setting]))
            {
                throw new InvalidOperationException(
                    $"Required configuration setting '{setting}' is missing for service '{serviceName}'");
            }
        }

        // Validate MongoDB connection string format
        var mongoConnectionString = configuration["MongoDB:ConnectionString"];
        if (!mongoConnectionString.StartsWith("mongodb://") && !mongoConnectionString.StartsWith("mongodb+srv://"))
        {
            throw new InvalidOperationException(
                $"MongoDB connection string must start with 'mongodb://' or 'mongodb+srv://' for service '{serviceName}'");
        }

        // Validate Kafka bootstrap servers
        var kafkaServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrEmpty(kafkaServers))
        {
            throw new InvalidOperationException(
                $"Kafka bootstrap servers must be configured for service '{serviceName}'");
        }
    }
}

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int MinConnectionPoolSize { get; set; } = 5;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool UseTls { get; set; } = true;
    public string? TlsCertificateFile { get; set; }
}

/// <summary>
/// Kafka configuration settings
/// </summary>
public class KafkaConfiguration
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = string.Empty;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public int SessionTimeoutMs { get; set; } = 30000;
    public int HeartbeatIntervalMs { get; set; } = 10000;
    public bool AutoOffsetReset { get; set; } = true;
    public bool EnableAutoCommit { get; set; } = false;
}

/// <summary>
/// Service-specific configuration
/// </summary>
public class ServiceConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public int Port { get; set; } = 8080;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentRequests { get; set; } = 1000;
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthenticationConfiguration
{
    public string MasterToken { get; set; } = string.Empty;
    public string Issuer { get; set; } = "io-platform";
    public string Audience { get; set; } = "io-platform-services";
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromHours(1);
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
}

/// <summary>
/// Configuration extensions for service registration
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Get validated configuration section
    /// </summary>
    public static T GetValidatedSection<T>(this IConfiguration configuration, string sectionName) where T : class
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section '{sectionName}' is missing");
        }

        var config = section.Get<T>();
        if (config == null)
        {
            throw new InvalidOperationException($"Failed to bind configuration section '{sectionName}' to type {typeof(T).Name}");
        }

        return config;
    }

    /// <summary>
    /// Check if running in development environment
    /// </summary>
    public static bool IsDevelopment(this IConfiguration configuration)
    {
        return configuration["ASPNETCORE_ENVIRONMENT"]?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Check if running in production environment
    /// </summary>
    public static bool IsProduction(this IConfiguration configuration)
    {
        return configuration["ASPNETCORE_ENVIRONMENT"]?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
