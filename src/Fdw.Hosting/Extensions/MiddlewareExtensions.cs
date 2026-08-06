using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Logging;
using Fdw.Hosting.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Fdw.Hosting.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline.
    /// This should be added early in the pipeline to catch exceptions from all subsequent middleware.
    /// Requires <see cref="Models.SupportOptions"/> to be configured via DI (empty defaults if not configured).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fdw.Hosting.Middleware");
        HostingLog.GlobalExceptionHandlerEnabled(logger);

        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }

    /// <summary>
    /// Adds standard FDW middleware pipeline: global exception handler, HTTPS redirection,
    /// security headers, and Serilog request logging.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static WebApplication UseFrameworkMiddleware(
        this WebApplication app,
        SecurityHeadersOptions? securityHeaders = null,
        ResponseBufferingOptions? responseBuffering = null)
    {
        // Global exception handler FIRST - catches all downstream exceptions
        app.UseGlobalExceptionHandler();

        app.UseHttpsRedirection();

        // Response buffering AFTER the exception handler so the handler's body writes also
        // benefit from explicit Content-Length, but BEFORE security headers / endpoint handlers
        // so every downstream response flows through the buffer. No-op when disabled.
        var bufferingOptions = responseBuffering
            ?? app.Configuration.GetSection("ResponseBuffering").Get<ResponseBufferingOptions>()
            ?? new ResponseBufferingOptions();
        app.UseFrameworkResponseBuffering(bufferingOptions);

        var resolvedOptions = securityHeaders ?? new SecurityHeadersOptions();
        app.UseMiddleware<SecurityHeadersMiddleware>(resolvedOptions);

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fdw.Hosting.Middleware");
        HostingLog.SecurityHeadersEnabled(logger);

        app.UseSerilogRequestLogging();

        return app;
    }

    /// <summary>
    /// Adds the <see cref="RequestContextMiddleware"/> to the pipeline.
    /// Must be called AFTER <c>UseAuthentication()</c> and <c>UseAuthorization()</c>
    /// so that <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> is populated.
    /// </summary>
    // Why: Placing this after authentication ensures ClaimsPrincipal is validated before
    // we extract tenant/role claims into IRequestContext. Calling it before auth would
    // always produce GuestContext regardless of the bearer token.
    [ExcludeFromCodeCoverage]
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestContextMiddleware>();
    }
}
