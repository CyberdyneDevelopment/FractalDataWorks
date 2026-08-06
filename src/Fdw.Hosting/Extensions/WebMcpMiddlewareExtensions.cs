using System.Diagnostics.CodeAnalysis;
using Fdw.Hosting.WebMcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Hosting.Extensions;

/// <summary>
/// Middleware extensions for WebMCP agent key authentication.
/// </summary>
public static class WebMcpMiddlewareExtensions
{
    /// <summary>
    /// Adds WebMCP agent key authentication middleware. Requests carrying a valid
    /// <c>X-Webmcp-Key</c> header (or the configured header name) are authenticated
    /// as the associated user with agent claims. Requests without the header pass
    /// through unmodified.
    /// </summary>
    /// <remarks>
    /// Call this before <c>UseAuthentication()</c> and <c>UseAuthorization()</c> so that
    /// the agent principal is in place when authorization policies evaluate.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseWebMcpApiKey(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<WebMcpOptions>();
        return app.UseMiddleware<WebMcpApiKeyMiddleware>(options);
    }
}
