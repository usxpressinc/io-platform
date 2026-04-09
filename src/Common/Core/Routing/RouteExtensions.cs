using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IO.Platform.Common.Core.Routing;

/// <summary>
/// Base extensions for organizing minimal API routes
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Creates a standard API group with authorization and tags
    /// </summary>
    public static RouteGroupBuilder CreateApiGroup(this IEndpointRouteBuilder routes, string prefix, string tag, string displayName)
    {
        return routes.MapGroup($"{prefix}")
            .RequireAuthorization()
            .WithTags(tag)
            .WithDisplayName(displayName);
    }
}