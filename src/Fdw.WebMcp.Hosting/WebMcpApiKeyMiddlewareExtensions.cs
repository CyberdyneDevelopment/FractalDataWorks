using System.Diagnostics.CodeAnalysis;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Extension methods for registering the WebMCP PAT authentication middleware.
/// </summary>
public static class WebMcpApiKeyMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="WebMcpApiKeyMiddleware"/> to the application pipeline.
    /// Place this before <c>UseAuthentication</c> so that PAT-bearing requests are
    /// resolved before the JWT bearer handler runs.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseWebMcpApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WebMcpApiKeyMiddleware>();
    }
}
