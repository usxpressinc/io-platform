using IO.Clara.Infrastructure.Highway;

namespace IO.Clara.App;

/// <summary>
/// Application dependency injection for IO.Clara
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add application services to the dependency injection container
    /// </summary>
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        // Add Highway API client
        builder.Services.AddScoped<IHighwayApiClient, HighwayApiClient>();

        return builder;
    }
}
