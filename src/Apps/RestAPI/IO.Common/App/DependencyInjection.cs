using IO.Common.App;
using IO.Common.Infrastructure.Email;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using Serilog;

namespace IO.Common.App;

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
        // Configure middleware pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "IO Common API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Add monitoring endpoints (includes health/ready automatically)
        app.MonitoringEndpoints();

        return app;
    }

    /// <summary>
    /// Adds API services and configurations to the provided <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">Host Builder.</param>
    public static void AddApplication(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = Enum.Parse<MonitoringEnvironment>(
            configuration.GetValue<string>("ENVIRONMENT") ?? "development",
            ignoreCase: true);
        var project = configuration.GetValue<string>("PROJECT") ?? "io-platform";
        var group = configuration.GetValue<string>("GROUP") ?? "common";

        // Add monitoring (Grafana/OTEL)
        builder.AddMonitoring(new MonitoringOptions
        {
            ProjectGroup = group,
            ProjectName = project,
            Environment = environment,
            ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
            EnableOtel = true,
            SerilogLoggerConfiguration = new LoggerConfiguration().WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"),
        });

        // Add email service
        builder.Services.AddSingleton<EmailService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "IO Common API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "X-Auth token (Bearer token or X-Auth-Token header)",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}
