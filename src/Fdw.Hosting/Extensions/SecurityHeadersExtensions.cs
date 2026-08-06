using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Fdw.Hosting.Extensions;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds security headers middleware to the pipeline.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseFrameworkSecurityHeaders(
        this IApplicationBuilder app,
        SecurityHeadersOptions? options = null)
    {
        var resolvedOptions = options ?? new SecurityHeadersOptions();
        return app.UseMiddleware<SecurityHeadersMiddleware>(resolvedOptions);
    }
}
