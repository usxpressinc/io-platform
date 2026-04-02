namespace IO.Elsa.Core;

/// <summary>
/// Dependency Injection extensions for Core services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds core services and configurations to the provided <see cref="IHostApplicationBuilder"/>.
    /// This includes registering core services for dependency injection and configuring CORS policies.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add core services and configurations to.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    public static IHostApplicationBuilder AddCore(this IHostApplicationBuilder builder) => builder
            .AddServices()
            .AddCors();

    /// <summary>
    /// Registers essential services to the provided <see cref="IHostApplicationBuilder"/> for dependency injection.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to which services will be added.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    private static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        // Add core services here as needed
        return builder;
    }

    /// <summary>
    /// Configures Cross-Origin Resource Sharing (CORS) policies for the provided <see cref="IHostApplicationBuilder"/>.
    /// This method sets up CORS to allow specified origins, headers, methods, and credentials.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> where the CORS policies will be configured.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance with the configured CORS policies.</returns>
    private static IHostApplicationBuilder AddCors(this IHostApplicationBuilder builder)
    {
        // Add CORS
        var origins = builder.Configuration["AllowedOrigins"];
        Console.WriteLine("Allowing Origins: " + origins);
        var ors = origins?.Split(";") ?? [];
        foreach (var o in ors)
        {
            Console.WriteLine("Allowing Origin: " + o);
        }

        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(ors)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return builder;
    }
}
