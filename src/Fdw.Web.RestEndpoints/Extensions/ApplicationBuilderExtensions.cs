using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fdw.Web.RestEndpoints.Configuration;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Web.RestEndpoints.Extensions;

/// <summary>
/// Extension methods for configuring the Fdw Web Framework middleware pipeline.
/// Provides fluent API for adding framework middleware to the ASP.NET Core pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the complete Fdw Web Framework middleware pipeline.
    /// Configures all required middleware in the correct order.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksWeb(this IApplicationBuilder app)
    {
        return app.UseFractalDataWorksWeb(null);
    }

    /// <summary>
    /// Adds the Fdw Web Framework middleware pipeline with custom configuration.
    /// Allows for configuration-driven middleware setup.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <param name="configureOptions">Action to configure the middleware options.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksWeb(
        this IApplicationBuilder app,
        Action<GenericWebMiddlewareOptions>? configureOptions = null)
    {
        var options = new GenericWebMiddlewareOptions();
        configureOptions?.Invoke(options);

        if (!ValidateRequiredServices(app))
            return app;

        var configuration = app.ApplicationServices.GetService<IOptions<WebConfiguration>>()?.Value
            ?? new WebConfiguration();

        LogMiddlewareConfiguration(app, configuration);

        return ConfigureMiddlewarePipeline(app, options);
    }

    /// <summary>
    /// Adds request validation middleware to the pipeline.
    /// Should be added early in the pipeline to validate incoming requests.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksRequestValidation(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength > EndpointDefaults.DefaultMaxRequestBodySize)
            {
                var logger = context.RequestServices.GetService<ILoggerFactory>()
                    ?.CreateLogger(typeof(ApplicationBuilderExtensions));
                if (logger is not null)
                {
                    MiddlewareLogger.RequestValidationFailed(logger,
                        $"Request body size {context.Request.ContentLength} exceeds maximum {EndpointDefaults.DefaultMaxRequestBodySize}");
                }

                context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                return;
            }

            var method = context.Request.Method;
            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase))
            {
                var contentType = context.Request.ContentType;
                if (string.IsNullOrEmpty(contentType))
                {
                    var logger = context.RequestServices.GetService<ILoggerFactory>()
                        ?.CreateLogger(typeof(ApplicationBuilderExtensions));
                    if (logger is not null)
                    {
                        MiddlewareLogger.RequestValidationFailed(logger,
                            "Content-Type header is required for POST/PUT requests");
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                    return;
                }
            }

            await next().ConfigureAwait(false);
        });

        return app;
    }

    /// <summary>
    /// Adds endpoint processing middleware to the pipeline.
    /// This is the core middleware that handles endpoint routing and execution.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksEndpoints(this IApplicationBuilder app)
    {
        app.UseFastEndpoints();
        return app;
    }

    /// <summary>
    /// Adds security headers middleware to the pipeline.
    /// Adds standard security headers to all responses.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksSecurityHeaders(this IApplicationBuilder app)
    {
        var securityConfig = app.ApplicationServices.GetService<IOptions<SecurityConfiguration>>()?.Value;
        var headers = securityConfig?.SecurityHeaders ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Content-Type-Options"] = "nosniff",
            ["X-Frame-Options"] = "DENY",
            ["X-XSS-Protection"] = "1; mode=block",
            ["Referrer-Policy"] = "strict-origin-when-cross-origin"
        };

        app.Use(async (context, next) =>
        {
            foreach (var header in headers)
            {
                context.Response.Headers[header.Key] = header.Value;
            }

            await next().ConfigureAwait(false);
        });

        return app;
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline.
    /// Enforces rate limiting policies defined in endpoint configurations.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }

    /// <summary>
    /// Adds authentication middleware to the pipeline.
    /// Handles authentication based on endpoint security requirements.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        return app;
    }

    /// <summary>
    /// Adds authorization middleware to the pipeline.
    /// Handles authorization checks based on endpoint security requirements.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// Adds CORS middleware to the pipeline.
    /// Configures CORS based on the web framework configuration.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksCors(this IApplicationBuilder app)
    {
        app.UseCors();
        return app;
    }

    /// <summary>
    /// Adds exception handling middleware to the pipeline.
    /// Provides centralized exception handling for the framework.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(handler =>
        {
            handler.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var logger = context.RequestServices.GetService<ILoggerFactory>()
                    ?.CreateLogger(typeof(ApplicationBuilderExtensions));

                if (logger is not null && exception is not null)
                {
                    MiddlewareLogger.UnhandledException(logger, exception);
                }

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "An unexpected error occurred.",
                    statusCode = context.Response.StatusCode
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response)).ConfigureAwait(false);
            });
        });

        return app;
    }

    /// <summary>
    /// Adds request/response logging middleware to the pipeline.
    /// Provides detailed logging of HTTP requests and responses.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksRequestResponseLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var stopwatch = Stopwatch.StartNew();

            await next().ConfigureAwait(false);

            stopwatch.Stop();

            var logger = context.RequestServices.GetService<ILoggerFactory>()
                ?.CreateLogger(typeof(ApplicationBuilderExtensions));

            if (logger is not null)
            {
                MiddlewareLogger.RequestCompleted(
                    logger,
                    context.Request.Method,
                    context.Request.Path.Value ?? "/",
                    stopwatch.Elapsed.TotalMilliseconds,
                    context.Response.StatusCode);
            }
        });

        return app;
    }

    /// <summary>
    /// Adds performance monitoring middleware to the pipeline.
    /// Collects performance metrics for endpoints and system health.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksPerformanceMonitoring(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var stopwatch = Stopwatch.StartNew();

            await next().ConfigureAwait(false);

            stopwatch.Stop();

            context.Response.Headers["X-Response-Time"] = $"{stopwatch.Elapsed.TotalMilliseconds:F1}ms";
        });

        return app;
    }

    /// <summary>
    /// Adds health check endpoints to the application.
    /// Provides standard health check endpoints for monitoring.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseFractalDataWorksHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");

        var logger = app.ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(ApplicationBuilderExtensions));
        if (logger is not null)
        {
            MiddlewareLogger.HealthCheckRegistered(logger, "/health");
        }

        return app;
    }

    /// <summary>
    /// Validates the middleware configuration and logs any issues.
    /// Should be called after all middleware has been configured.
    /// </summary>
    /// <param name="app">The application builder to validate.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder ValidateFractalDataWorksWebConfiguration(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetService<IOptions<WebConfiguration>>()?.Value;
        if (configuration is null)
            return app;

        var logger = app.ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(ApplicationBuilderExtensions));
        if (logger is null)
            return app;

        if (string.IsNullOrEmpty(configuration.Host))
        {
            MiddlewareLogger.ConfigurationWarning(logger, "Host is not configured");
        }

        if (configuration.Port < 1 || configuration.Port > 65535)
        {
            MiddlewareLogger.ConfigurationWarning(logger,
                $"Port {configuration.Port} is outside valid range (1-65535)");
        }

        if (configuration.ForceHttps && string.IsNullOrEmpty(configuration.SslCertificatePath))
        {
            MiddlewareLogger.ConfigurationWarning(logger,
                "ForceHttps is enabled but no SSL certificate path is configured");
        }

        return app;
    }

    /// <summary>
    /// Configures the middleware pipeline based on the middleware options.
    /// Automatically configures middleware based on option settings and ordering.
    /// </summary>
    private static IApplicationBuilder ConfigureMiddlewarePipeline(
        IApplicationBuilder app,
        GenericWebMiddlewareOptions options)
    {
        var enabledCount = 0;

        foreach (var middleware in options.MiddlewareOrder)
        {
            var applied = ApplyMiddleware(app, middleware, options);
            if (applied)
                enabledCount++;
        }

        var logger = app.ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(ApplicationBuilderExtensions));
        if (logger is not null)
        {
            MiddlewareLogger.MiddlewarePipelineConfigured(logger, enabledCount);
        }

        return app;
    }

    private static bool ApplyMiddleware(
        IApplicationBuilder app,
        string middlewareName,
        GenericWebMiddlewareOptions options)
    {
        switch (middlewareName)
        {
            case "ExceptionHandling" when options.EnableExceptionHandling:
                app.UseFractalDataWorksExceptionHandling();
                return true;
            case "SecurityHeaders" when options.EnableSecurityHeaders:
                app.UseFractalDataWorksSecurityHeaders();
                return true;
            case "Cors" when options.EnableCors:
                app.UseFractalDataWorksCors();
                return true;
            case "RequestValidation" when options.EnableRequestValidation:
                app.UseFractalDataWorksRequestValidation();
                return true;
            case "Authentication" when options.EnableAuthentication:
                app.UseFractalDataWorksAuthentication();
                return true;
            case "Authorization" when options.EnableAuthorization:
                app.UseFractalDataWorksAuthorization();
                return true;
            case "RateLimiting" when options.EnableRateLimiting:
                app.UseFractalDataWorksRateLimiting();
                return true;
            case "PerformanceMonitoring" when options.EnablePerformanceMonitoring:
                app.UseFractalDataWorksPerformanceMonitoring();
                return true;
            case "RequestResponseLogging" when options.EnableRequestResponseLogging:
                app.UseFractalDataWorksRequestResponseLogging();
                return true;
            case "Endpoints":
                app.UseFractalDataWorksEndpoints();
                return true;
            case "HealthChecks" when options.EnableHealthChecks:
                app.UseFractalDataWorksHealthChecks();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Validates that required services are registered.
    /// Ensures all dependencies are available before configuring middleware.
    /// </summary>
    private static bool ValidateRequiredServices(IApplicationBuilder app)
    {
        var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
        if (loggerFactory is null)
            return false;

        var logger = loggerFactory.CreateLogger(typeof(ApplicationBuilderExtensions));
        var isValid = true;

        if (app.ApplicationServices.GetService<ILoggerFactory>() is null)
        {
            MiddlewareLogger.RequiredServiceMissing(logger, nameof(ILoggerFactory));
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Logs the middleware configuration for debugging.
    /// Provides visibility into the configured middleware pipeline.
    /// </summary>
    private static void LogMiddlewareConfiguration(IApplicationBuilder app, WebConfiguration configuration)
    {
        var logger = app.ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(ApplicationBuilderExtensions));
        if (logger is null)
            return;

        MiddlewareLogger.MiddlewareState(logger, "Host", configuration.Host);
        MiddlewareLogger.MiddlewareState(logger, "Port", configuration.Port.ToString(CultureInfo.InvariantCulture));
        MiddlewareLogger.MiddlewareState(logger, "ForceHttps", configuration.ForceHttps ? "enabled" : "disabled");
    }
}
