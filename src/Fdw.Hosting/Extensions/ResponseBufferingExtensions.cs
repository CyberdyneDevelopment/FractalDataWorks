using System.Diagnostics.CodeAnalysis;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Fdw.Hosting.Extensions;

/// <summary>
/// Extension methods for the response-buffering middleware.
/// </summary>
public static class ResponseBufferingExtensions
{
    /// <summary>
    /// Adds the framework response-buffering middleware. No-op unless
    /// <see cref="ResponseBufferingOptions.Enabled"/> is true on the supplied options.
    /// </summary>
    // Why: matches the SecurityHeaders pattern — extension method takes typed options so the
    // caller controls when it's wired. UseFrameworkMiddleware reads the appsettings section
    // and calls this; apps with no "ResponseBuffering" section get a disabled instance and the
    // middleware short-circuits in InvokeAsync.
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseFrameworkResponseBuffering(
        this IApplicationBuilder app,
        ResponseBufferingOptions? options = null)
    {
        var resolvedOptions = options ?? new ResponseBufferingOptions();
        return app.UseMiddleware<ResponseBufferingMiddleware>(resolvedOptions);
    }
}
