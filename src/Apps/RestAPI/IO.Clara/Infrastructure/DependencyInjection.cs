using IO.Clara.Infrastructure.Highway;

namespace IO.Clara.Infrastructure;

/// <summary>
/// Infrastructure dependency injection for IO.Clara
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add infrastructure services to the dependency injection container
    /// </summary>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Add Highway API client
        builder.Services.AddHttpClient<IHighwayApiClient, HighwayApiClient>();

        return builder;
    }
}
