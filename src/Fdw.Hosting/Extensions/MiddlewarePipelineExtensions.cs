using System;
using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using Fdw.Hosting.Configuration;
using Fdw.Services.Multitenancy.Sql.Extensions;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Hosting.Extensions;

/// <summary>
/// Extension methods for configuring the full HTTP middleware pipeline in correct order.
/// </summary>
[ExcludeFromCodeCoverage]
public static class MiddlewarePipelineExtensions
{
    
    /// <summary>
    /// Calls <c>UseFastEndpoints</c> with <see cref="PermissionClaimsPreProcessor"/> registered
    /// as a global pre-processor on every endpoint, then invokes the caller-supplied configuration
    /// action for app-specific settings (route prefix, error builders, etc.).
    /// </summary>
    /// <remarks>
    /// Use this instead of calling <c>app.UseFastEndpoints(config => ...)</c> directly.
    /// Apps that use FDW endpoint base classes (<see cref="ProtectedEndpointBase{TResponse}"/>,
    /// <see cref="AdminEndpointBase{TResponse}"/>) already get the pre-processor via the base class
    /// <c>Configure()</c> override. Use <c>UseFdwFastEndpoints</c> to cover endpoints that extend
    /// FastEndpoints' <c>Endpoint&lt;T&gt;</c> directly (e.g. ETL, Scheduler endpoints).
    /// </remarks>
    /// <param name="app">The web application.</param>
    /// <param name="configure">
    /// Optional action to supply app-specific FastEndpoints config (route prefix, security options,
    /// error builder, etc.). Called after the global pre-processor configurator is applied.
    /// </param>
    public static WebApplication UseFdwFastEndpoints(
        this WebApplication app,
        Action<Config>? configure = null)
    {
        app.UseFastEndpoints(config =>
        {
            // Why: Register PermissionClaimsPreProcessor on every endpoint via the global Configurator.
            // This covers bare FastEndpoints Endpoint<T> subclasses in ETL/Scheduler that do not
            // extend FDW base classes (ProtectedEndpointBase/AdminEndpointBase already add the
            // pre-processor via Definition.PreProcessors inside their own Configure() override).
            // EndpointOptions.Configurator is write-only; we set it first, then the caller's
            // configure action may override it with their own Configurator if needed.
            // Convention: callers that set config.Endpoints.Configurator inside configure() should
            // compose the pre-processor inside their own Configurator lambda.
            config.Endpoints.Configurator = ep =>
            {
                ep.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());
            };

            // Apply the caller's app-specific settings (RoutePrefix, Security, Errors, etc.).
            // If the caller sets their own Configurator here, it overrides the one above.
            // In that case, the caller is responsible for adding PermissionClaimsPreProcessor
            // inside their Configurator if they want it on bare Endpoint<T> subclasses.
            configure?.Invoke(config);
        });

        return app;
    }
}
