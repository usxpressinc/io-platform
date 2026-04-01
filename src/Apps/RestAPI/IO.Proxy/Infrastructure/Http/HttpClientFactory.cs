using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace IO.Proxy.Infrastructure.Http;

/// <summary>
/// HTTP client factory for downstream service communication
/// Provides resilience patterns, authentication, and monitoring
/// </summary>
public static class HttpClientFactory
{
    /// <summary>
    /// Configure HTTP clients for downstream services
    /// </summary>
    public static IServiceCollection AddGatewayHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure HTTP settings
        services.Configure<HttpSettings>(configuration.GetSection("Gateway:Http"));

        // Register HTTP client factory
        services.AddSingleton<IHttpClientFactoryService, HttpClientFactoryService>();

        // Configure individual service HTTP clients
        services.AddHttpClient("io-common", client =>
        {
            client.BaseAddress = new Uri("http://io-common:8081");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());

        services.AddHttpClient("io-cass", client =>
        {
            client.BaseAddress = new Uri("http://io-cass:8082");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());

        services.AddHttpClient("io-elsa", client =>
        {
            client.BaseAddress = new Uri("http://io-elsa:8083");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());

        return services;
    }

    /// <summary>
    /// Get retry policy for HTTP requests
    /// </summary>
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    context.GetLogger()?.LogWarning(
                        "Request failed with {StatusCode}. Retrying in {Delay}s (attempt {Attempt}/{MaxAttempts})",
                        outcome.Result?.StatusCode,
                        timespan.TotalSeconds,
                        retryAttempt,
                        3);
                });
    }

    /// <summary>
    /// Get circuit breaker policy for HTTP requests
    /// </summary>
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                onBreak: (outcome, timespan, context) =>
                {
                    context.GetLogger()?.LogError(
                        "Circuit breaker opened due to consecutive failures. Circuit will be open for {Duration}",
                        timespan);
                },
                onReset: (context) =>
                {
                    context.GetLogger()?.LogInformation("Circuit breaker reset");
                });
    }

    /// <summary>
    /// Get timeout policy for HTTP requests
    /// </summary>
    private static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }
}

/// <summary>
/// HTTP client factory service interface
/// </summary>
public interface IHttpClientFactoryService
{
    HttpClient CreateClient(string serviceName);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string serviceName);
    Task<T> SendAsync<T>(HttpRequestMessage request, string serviceName);
}

/// <summary>
/// HTTP client factory service implementation
/// </summary>
public class HttpClientFactoryService : IHttpClientFactoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpMetrics _metrics;
    private readonly ILogger<HttpClientFactoryService> _logger;

    public HttpClientFactoryService(
        IHttpClientFactory httpClientFactory,
        IHttpMetrics metrics,
        ILogger<HttpClientFactoryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _logger = logger;
    }

    public HttpClient CreateClient(string serviceName)
    {
        var clientName = GetClientName(serviceName);
        var client = _httpClientFactory.CreateClient(clientName);

        // Add default headers
        client.DefaultRequestHeaders.Add("User-Agent", "IO-Proxy/1.0");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", GetClientIp());

        return client;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string serviceName)
    {
        var client = CreateClient(serviceName);
        var startTime = DateTime.UtcNow;

        try
        {
            var response = await client.SendAsync(request);
            var duration = DateTime.UtcNow - startTime;

            // Record metrics
            _metrics.RecordRequest(serviceName, request.Method.ToString(), (int)response.StatusCode, duration);

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordError(serviceName, request.Method.ToString(), duration);
            
            _logger.LogError(ex, "HTTP request to {ServiceName} failed", serviceName);
            throw;
        }
    }

    public async Task<T> SendAsync<T>(HttpRequestMessage request, string serviceName)
    {
        var response = await SendAsync(request, serviceName);
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(content) 
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private string GetClientName(string serviceName)
    {
        return serviceName.ToLowerInvariant() switch
        {
            "io.common" or "common" => "io-common",
            "io.cass" or "cass" => "io-cass",
            "io.elsa" or "elsa" => "io-elsa",
            _ => serviceName
        };
    }

    private string GetClientIp()
    {
        // This would typically come from HttpContext
        return "127.0.0.1";
    }
}

/// <summary>
/// HTTP settings configuration
/// </summary>
public class HttpSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
}

/// <summary>
/// HTTP metrics interface
/// </summary>
public interface IHttpMetrics
{
    void RecordRequest(string serviceName, string method, int statusCode, TimeSpan duration);
    void RecordError(string serviceName, string method, TimeSpan duration);
    HttpServiceMetrics GetMetrics(string serviceName);
}

/// <summary>
/// HTTP metrics implementation
/// </summary>
public class HttpMetrics : IHttpMetrics
{
    private readonly ConcurrentDictionary<string, HttpServiceMetrics> _metrics;
    private readonly ILogger<HttpMetrics> _logger;

    public HttpMetrics(ILogger<HttpMetrics> logger)
    {
        _metrics = new ConcurrentDictionary<string, HttpServiceMetrics>();
        _logger = logger;
    }

    public void RecordRequest(string serviceName, string method, int statusCode, TimeSpan duration)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new HttpServiceMetrics
        {
            ServiceName = serviceName
        });

        metrics.TotalRequests++;
        metrics.ResponseTimeSum += duration.TotalMilliseconds;
        metrics.LastRequest = DateTime.UtcNow;

        // Update status code counts
        metrics.StatusCodeCounts.AddOrUpdate(statusCode.ToString(), 1, (_, count) => count + 1);

        // Update success/failure counts
        if (statusCode >= 200 && statusCode < 300)
        {
            metrics.SuccessfulRequests++;
        }
        else
        {
            metrics.FailedRequests++;
        }

        // Update method counts
        metrics.MethodCounts.AddOrUpdate(method, 1, (_, count) => count + 1);
    }

    public void RecordError(string serviceName, string method, TimeSpan duration)
    {
        var metrics = _metrics.GetOrAdd(serviceName, _ => new HttpServiceMetrics
        {
            ServiceName = serviceName
        });

        metrics.TotalErrors++;
        metrics.LastError = DateTime.UtcNow;
        metrics.MethodCounts.AddOrUpdate(method, 1, (_, count) => count + 1);
    }

    public HttpServiceMetrics GetMetrics(string serviceName)
    {
        return _metrics.TryGetValue(serviceName, out var metrics) ? metrics : new HttpServiceMetrics
        {
            ServiceName = serviceName
        };
    }
}

/// <summary>
/// HTTP service metrics
/// </summary>
public class HttpServiceMetrics
{
    public string ServiceName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int TotalErrors { get; set; }
    public double ResponseTimeSum { get; set; }
    public DateTime LastRequest { get; set; }
    public DateTime? LastError { get; set; }
    public Dictionary<string, int> StatusCodeCounts { get; set; } = new();
    public Dictionary<string, int> MethodCounts { get; set; } = new();

    public double AverageResponseTime => TotalRequests > 0 ? ResponseTimeSum / TotalRequests : 0;
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    public double ErrorRate => TotalRequests > 0 ? (double)TotalErrors / TotalRequests : 0;
}

/// <summary>
/// Extension methods for logging
/// </summary>
internal static class ContextExtensions
{
    public static ILogger? GetLogger(this Context context)
    {
        // This would need to be properly implemented with DI
        return null;
    }
}
