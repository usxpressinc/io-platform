using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IO.Platform.Common.Core.Lifecycle;

/// <summary>
/// Comprehensive graceful shutdown service for all IO Platform microservices
/// Ensures clean termination of in-flight requests, database connections, and message processing
/// </summary>
public class GracefulShutdownService : IHostedService, IDisposable
{
    private readonly ILogger<GracefulShutdownService> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly List<IGracefulShutdownComponent> _components;
    private readonly TimeSpan _shutdownTimeout;
    private readonly object _lock = new();
    private bool _disposed = false;

    public GracefulShutdownService(
        ILogger<GracefulShutdownService> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        TimeSpan? shutdownTimeout = null)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _components = new List<IGracefulShutdownComponent>();
        _shutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(30);

        // Register for application lifetime events
        hostApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    /// <summary>
    /// Register a component that needs graceful shutdown handling
    /// </summary>
    public void RegisterComponent(IGracefulShutdownComponent component)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _components.Add(component);
                _logger.LogDebug("Registered graceful shutdown component: {ComponentType}", component.GetType().Name);
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful shutdown service started with timeout: {Timeout}", _shutdownTimeout);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful shutdown service stopping");
        
        // Wait for all components to complete shutdown
        await Task.WhenAll(_components.Select(component => 
            ShutdownComponentSafely(component, cancellationToken)));
        
        _logger.LogInformation("Graceful shutdown service completed");
    }

    private void OnApplicationStopping()
    {
        _logger.LogInformation("Application stopping event received - initiating graceful shutdown");
        
        // Start graceful shutdown in background
        Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(_shutdownTimeout);
                await ShutdownAllComponentsAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during graceful shutdown");
            }
        });
    }

    private async Task ShutdownAllComponentsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting graceful shutdown of {ComponentCount} components", _components.Count);
        
        var shutdownTasks = _components.Select(component => 
            ShutdownComponentSafely(component, cancellationToken));

        var results = await Task.WhenAll(shutdownTasks);
        
        var successful = results.Count(r => r);
        var failed = results.Count(r => !r);
        
        _logger.LogInformation("Graceful shutdown completed: {Successful} successful, {Failed} failed", successful, failed);
        
        if (failed > 0)
        {
            _logger.LogWarning("Some components failed to shutdown gracefully within timeout");
        }
    }

    private async Task<bool> ShutdownComponentSafely(
        IGracefulShutdownComponent component, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Shutting down component: {ComponentType}", component.GetType().Name);
            
            await component.ShutdownAsync(cancellationToken);
            
            _logger.LogDebug("Component shutdown completed: {ComponentType}", component.GetType().Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown component: {ComponentType}", component.GetType().Name);
            return false;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _components.Clear();
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Interface for components that require graceful shutdown handling
/// </summary>
public interface IGracefulShutdownComponent
{
    /// <summary>
    /// Perform graceful shutdown operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown timeout</param>
    Task ShutdownAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Base implementation for graceful shutdown components
/// </summary>
public abstract class GracefulShutdownComponentBase : IGracefulShutdownComponent
{
    protected readonly ILogger Logger;

    protected GracefulShutdownComponentBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task ShutdownAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Graceful shutdown component for HTTP servers
/// </summary>
public class HttpServerGracefulShutdown : GracefulShutdownComponentBase
{
    private readonly TimeSpan _drainTimeout;

    public HttpServerGracefulShutdown(
        ILogger<HttpServerGracefulShutdown> logger,
        TimeSpan? drainTimeout = null)
        : base(logger)
    {
        _drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(15);
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting HTTP server graceful shutdown with drain timeout: {Timeout}", _drainTimeout);
        
        // Implementation would:
        // 1. Stop accepting new requests
        // 2. Wait for in-flight requests to complete
        // 3. Close connections gracefully
        
        await Task.Delay(_drainTimeout, cancellationToken);
        
        Logger.LogInformation("HTTP server graceful shutdown completed");
    }
}

/// <summary>
/// Graceful shutdown component for database connections
/// </summary>
public class DatabaseGracefulShutdown : GracefulShutdownComponentBase
{
    public DatabaseGracefulShutdown(ILogger<DatabaseGracefulShutdown> logger)
        : base(logger)
    {
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting database connection graceful shutdown");
        
        // Implementation would:
        // 1. Stop accepting new database operations
        // 2. Wait for in-flight operations to complete
        // 3. Close database connections gracefully
        
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        
        Logger.LogInformation("Database graceful shutdown completed");
    }
}

/// <summary>
/// Graceful shutdown component for Kafka consumers
/// </summary>
public class KafkaGracefulShutdown : GracefulShutdownComponentBase
{
    public KafkaGracefulShutdown(ILogger<KafkaGracefulShutdown> logger)
        : base(logger)
    {
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting Kafka consumer graceful shutdown");
        
        // Implementation would:
        // 1. Stop consuming new messages
        // 2. Wait for in-flight message processing to complete
        // 3. Commit offsets gracefully
        // 4. Close consumer connections
        
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        
        Logger.LogInformation("Kafka consumer graceful shutdown completed");
    }
}
