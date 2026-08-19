using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Hosting extensions for WebMCP — the W3C browser standard that exposes structured tools
/// to AI agents via <c>navigator.modelContext.registerTool()</c>.
/// </summary>
public static class WebMcpServiceCollectionExtensions
{
    /// <summary>
    /// Registers WebMCP services. The tool registry will be populated from the supplied
    /// <paramref name="assemblies"/> when <see cref="MapWebMcp"/> is called during startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">
    ///   Assemblies to scan for endpoint classes decorated with <see cref="WebMcpToolAttribute"/>.
    ///   Pass at least the assembly that contains your endpoint implementations.
    /// </param>
    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddWebMcp(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddWebMcp(assemblies, configure: null);

    /// <summary>
    /// Registers WebMCP services with optional configuration. The tool registry will be
    /// populated from the supplied <paramref name="assemblies"/> when <see cref="MapWebMcp"/>
    /// is called during startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">
    ///   Assemblies to scan for endpoint classes decorated with <see cref="WebMcpToolAttribute"/>.
    ///   Pass at least the assembly that contains your endpoint implementations.
    /// </param>
    /// <param name="configure">
    ///   Optional delegate to configure <see cref="WebMcpOptions"/>, including API key
    ///   authentication settings.
    /// </param>
    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddWebMcp(
        this IServiceCollection services,
        Assembly[] assemblies,
        Action<WebMcpOptions>? configure = null)
    {
        var options = new WebMcpOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        var registry = new WebMcpToolRegistry();

        // Store the assembly list alongside the registry for deferred discovery in MapWebMcp
        services.AddSingleton(new WebMcpAssemblyList(assemblies));
        services.AddSingleton<IWebMcpToolRegistry>(registry);
        services.AddSingleton(registry);  // also as concrete type for internal use
        services.AddSingleton<WebMcpJsGenerator>();

        return services;
    }

    /// <summary>
    /// Triggers WebMCP tool discovery and maps the <c>GET /.well-known/webmcp.js</c> route.
    /// Call this after <c>app.Build()</c> alongside other <c>Map*</c> calls.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static WebApplication MapWebMcp(this WebApplication app)
    {
        var assemblyList = app.Services.GetRequiredService<WebMcpAssemblyList>();
        var registry = app.Services.GetRequiredService<WebMcpToolRegistry>();
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Fdw.Hosting.WebMcp");

        registry.Discover(assemblyList.Assemblies, logger);

        var generator = app.Services.GetRequiredService<WebMcpJsGenerator>();

        app.MapGet("/.well-known/webmcp.js", (HttpContext httpContext) =>
        {
            httpContext.Response.Headers.CacheControl = "public, max-age=3600";
            var js = generator.Generate();
            return Microsoft.AspNetCore.Http.Results.Content(js, "application/javascript");
        })
        .ExcludeFromDescription();

        return app;
    }
}
