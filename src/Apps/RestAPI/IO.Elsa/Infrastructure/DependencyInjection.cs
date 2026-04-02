using IO.Elsa.Core.Pricing;
using IO.Elsa.Infrastructure;

namespace IO.Elsa.Infrastructure;

/// <summary>
/// Dependency Injection extensions for Infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services and configurations to the provided <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add infrastructure services and configurations to.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Add HttpClient for Elsa pricing API
        builder.Services.AddHttpClient<IElsaPricingApiClient, ElsaPricingApiClient>();

        // Add SPAPI pricing service (simple proxy service)
        builder.Services.AddSingleton<ISpapiPricingService, SpapiPricingService>();

        return builder;
    }
}
