using IO.Elsa.App;
using IO.Elsa.Core;
using IO.Elsa.Infrastructure;
using IO.Elsa.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using Serilog;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Security;
using Microsoft.Identity.Web;
using Asp.Versioning;

namespace IO.Elsa.App;

/// <summary>
/// Dependency injection extensions for configuring services and routes.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures and maps API routes for the application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance used to define the HTTP pipeline and endpoints.</param>
    /// <returns>The modified <see cref="WebApplication"/> instance with mapped routes.</returns>
    public static WebApplication MapRoutes(this WebApplication app)
    {
        var project = app.Configuration[ApplicationOptions.Project];
        var clientId = app.Configuration["AUTH:ClientId"] ?? string.Empty;

        app.AddDocsRoutes(s =>
        {
            s.OAuth2Client = new OAuth2ClientSettings
            {
                ClientId = clientId,
                AppName = project,
            };
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.MonitoringEndpoints();

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var apiGroup = app.MapGroup("v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1, 0)
            .MapElsaRoutesV1();

        return app;
    }

    /// <summary>
    /// Adds API services and configurations to the provided <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">Host Builder.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AUTH");
        builder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                    new Asp.Versioning.UrlSegmentApiVersionReader(),
                    new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"));
            });
        builder.ConfigureMonitoring().AddSwagger().AddServices();
    }

    private static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        // Add graceful shutdown
        builder.Services.AddSingleton<IO.Platform.Common.Core.Lifecycle.IGracefulShutdownComponent, IO.Platform.Common.Core.Lifecycle.HttpServerGracefulShutdown>();
        builder.Services.AddHostedService<IO.Platform.Common.Core.Lifecycle.GracefulShutdownService>();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureMonitoring(this IHostApplicationBuilder builder)
    {
        var environment = Enum.Parse<MonitoringEnvironment>(
            builder.Configuration[ApplicationOptions.Environment] ?? "development",
            ignoreCase: true);
        var project = builder.Configuration[ApplicationOptions.Project] ?? "io-platform";
        var group = builder.Configuration[ApplicationOptions.Group] ?? "pricing";
        var version = builder.Configuration[ApplicationOptions.Revision] ?? "1.0.0";

        builder.AddMonitoring(
            new MonitoringOptions
            {
                ProjectGroup = group,
                ProjectName = project,
                Environment = environment,
                ReleaseVersion = version,
                EnableOtel = true,
                SerilogLoggerConfiguration = new LoggerConfiguration().WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"),
            });
        return builder;
    }

    private static IHostApplicationBuilder AddSwagger(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var clientId = configuration["AUTH:ClientId"] ?? string.Empty;
        var tenantId = configuration["AUTH:TenantId"] ?? string.Empty;

        // Add v1 API documentation
        builder.Services.AddDocsServices(document =>
        {
            document.DocumentName = "io-elsa-api-v1";
            document.Title = "IO Elsa API v1";
            document.Description = "Elsa Pricing API for IO platform - Version 1.0";
            document.ApiGroupNames = ["Pricing"];
            document.Version = "1.0";
            document.OperationProcessors.Add(new ApiVersionOperationProcessor("v1"));
            document.DocumentProcessors.Add(new ApiVersionDocumentProcessor("v1"));
            document.AddSecurity(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl =
                                $"https://login.microsoftonline.com/{configuration["AUTH:TenantId"]}/oauth2/v2.0/authorize",
                            Scopes = new Dictionary<string, string>
                            {
                                { $"{clientId}/.default", string.Empty },
                            },
                            TokenUrl =
                                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                        },
                    },
                });
            document.OperationProcessors.Add(
                new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
        return builder;
    }

    /// <summary>
    /// Operation processor to replace version parameter in operation paths.
    /// </summary>
    private sealed class ApiVersionOperationProcessor(string version) : IOperationProcessor
    {
        public bool Process(NSwag.Generation.Processors.Contexts.OperationProcessorContext context)
        {
            // Replace version parameters in the path
            var originalPath = context.OperationDescription.Path;
            var newPath = originalPath;

            // Handle various version parameter formats
            if (originalPath.Contains("{version") || originalPath.Contains("{apiVersion"))
            {
                newPath = originalPath
                    .Replace("{version:apiVersion}", version)
                    .Replace("{version}", version)
                    .Replace("{apiVersion}", version);
            }

            // Handle the case where API versioning has already partially processed it to /v/
            else if (originalPath.StartsWith("/v/", StringComparison.Ordinal))
            {
                newPath = originalPath.Replace("/v/", $"/{version}/");
            }

            if (newPath != originalPath)
            {
                context.OperationDescription.Path = newPath;
                Console.WriteLine($"[ApiVersionOperationProcessor] Replaced '{originalPath}' with '{newPath}'");
            }
            else
            {
                Console.WriteLine($"[ApiVersionOperationProcessor] No change needed for: '{originalPath}'");
            }

            return true;
        }
    }

    /// <summary>
    /// Document processor to filter operations by API version and replace route template with actual version.
    /// </summary>
    private sealed class ApiVersionDocumentProcessor(string version) : IDocumentProcessor
    {
        public void Process(NSwag.Generation.Processors.Contexts.DocumentProcessorContext context)
        {
            Console.WriteLine($"[ApiVersionDocumentProcessor] Processing {context.Document.Paths.Count} paths");

            // Replace version parameter placeholders with actual version in all paths
            var pathsToUpdate = context.Document.Paths.ToList();
            context.Document.Paths.Clear();

            foreach (var path in pathsToUpdate)
            {
                var newPath = path.Key;

                // Handle multiple possible version parameter formats
                if (path.Key.Contains("{version") || path.Key.Contains("{apiVersion"))
                {
                    newPath = path.Key
                        .Replace("{version:apiVersion}", version)
                        .Replace("{version}", version)
                        .Replace("{apiVersion}", version);
                }

                // Handle the case where API versioning has already partially processed it to /v/
                else if (path.Key.StartsWith("/v/", StringComparison.Ordinal))
                {
                    newPath = path.Key.Replace("/v/", $"/{version}/");
                }

                Console.WriteLine($"[ApiVersionDocumentProcessor] Path: '{path.Key}' -> '{newPath}'");
                context.Document.Paths[newPath] = path.Value;
            }
        }
    }
}
