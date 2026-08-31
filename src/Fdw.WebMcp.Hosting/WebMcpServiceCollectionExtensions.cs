using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.WebMcp.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Hosting extensions for WebMCP — the W3C browser standard that exposes structured tools
/// to AI agents via <c>document.modelContext.registerTool()</c>.
/// </summary>
public static class WebMcpServiceCollectionExtensions
{
    /// <summary>
    /// Registers WebMCP services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    ///   Optional delegate to configure <see cref="WebMcpOptions"/>, including API key
    ///   authentication settings.
    /// </param>
    /// <remarks>
    /// Takes no assemblies. Which endpoints are tools is decided by the endpoints themselves, in
    /// <see cref="WebMcpToolAttribute"/> on their options, and a host that had to name assemblies
    /// would be maintaining a second list that says the same thing and can disagree with it.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddWebMcp(
        this IServiceCollection services,
        Action<WebMcpOptions>? configure = null)
    {
        var options = new WebMcpOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        var registry = new WebMcpToolRegistry();
        services.AddSingleton<IWebMcpToolRegistry>(registry);
        services.AddSingleton(registry);  // also as concrete type for internal use
        services.AddSingleton<WebMcpJsGenerator>();

        return services;
    }

    /// <summary>
    /// Resolves the declared WebMCP tools against the application's routes and maps the
    /// <c>GET /.well-known/webmcp.js</c> route.
    /// </summary>
    /// <param name="app">The built application.</param>
    /// <remarks>
    /// ORDERING: call this AFTER the call that maps the endpoints — <c>MapFastEndpoints</c> — and
    /// after every domain has registered. It reads the route table to learn each tool's route, so
    /// called too early it finds no routes and serves a script with no tools.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static WebApplication MapWebMcp(this WebApplication app)
    {
        var registry = app.Services.GetRequiredService<WebMcpToolRegistry>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Fdw.Hosting.WebMcp");

        // Why the app's own data sources rather than a resolved EndpointDataSource: the composite is
        // not reliably resolvable from the container, and the route table this reads has to be the one
        // this application built -- a tool resolved against any other is a route that will not match.
        registry.Resolve(
            DeclaredWebMcpTools.Declarations,
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints).ToList(),
            logger);

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
