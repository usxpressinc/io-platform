using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace IO.Proxy.Routes;

/// <summary>
/// Static route definitions for Gateway API endpoints
/// </summary>
public static class GatewayRoutes
{
    /// <summary>
    /// Maps all proxy endpoints for downstream services
    /// </summary>
    public static IEndpointRouteBuilder MapGatewayRoutes(this IEndpointRouteBuilder routes)
    {
        var proxyGroup = routes.MapGroup("/api")
            .WithTags("Proxy")
            .WithDisplayName("IO.Proxy API");

        // Email proxy - https://api.demo.poc.dev.usxpress.io/api/common/email
        proxyGroup.MapPost("common/email", async (
            [FromBody] EmailRequest request,
            IHttpClientFactory httpClientFactory,
            ILogger logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-common");
                var response = await client.PostAsJsonAsync("/api/common/email", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(result);
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying email request");
                return Results.StatusCode(500);
            }
        })
        .WithName("ProxyEmail")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Carrier validation proxy - https://api.demo.poc.qa.usxpress.io/api/clara/carriers/valid
        proxyGroup.MapPost("clara/carriers/valid", async (
            [FromBody] CarrierValidationRequest request,
            IHttpClientFactory httpClientFactory,
            ILogger logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-cass");
                var response = await client.PostAsJsonAsync("/api/clara/carriers/valid", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(result);
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying carrier validation request");
                return Results.StatusCode(500);
            }
        })
        .WithName("ProxyCarrierValidation")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Pricing lookup proxy - https://api.demo.poc.qa.usxpress.io/api/elsa/price/lookup
        proxyGroup.MapPost("elsa/price/lookup", async (
            [FromBody] object request,
            IHttpClientFactory httpClientFactory,
            ILogger logger) =>
        {
            try
            {
                var client = httpClientFactory.CreateClient("io-elsa");
                var response = await client.PostAsJsonAsync("/api/elsa/price/lookup", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Results.Ok(result);
                }
                
                var error = await response.Content.ReadAsStringAsync();
                return Results.StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error proxying price lookup request");
                return Results.StatusCode(500);
            }
        })
        .WithName("ProxyPriceLookup")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        return routes;
    }
}

/// <summary>
/// Email request model matching the API specification
/// </summary>
public record EmailRequest
{
    public string body { get; init; } = string.Empty;
    public string from_email { get; init; } = string.Empty;
    public string to_emails { get; init; } = string.Empty;
    public string subject { get; init; } = string.Empty;
    public string cc_emails { get; init; } = string.Empty;
    public string bcc_emails { get; init; } = string.Empty;
    public string from_name { get; init; } = string.Empty;
    public string title { get; init; } = string.Empty;
}

/// <summary>
/// Carrier validation request model matching the API specification
/// </summary>
public record CarrierValidationRequest
{
    public string mcNumber { get; init; } = string.Empty;
    public string dotNumber { get; init; } = string.Empty;
    public string brokerageOrderId { get; init; } = string.Empty;
}
