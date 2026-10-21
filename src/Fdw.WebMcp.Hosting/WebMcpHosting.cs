using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.WebMcp.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Registers a host's WebMCP surface and serves it: the tool registry, the script generator and the
/// settings reader, then <c>GET /.well-known/webmcp.js</c>.
/// </summary>
/// <remarks>
/// <para>
/// A three-phase registrant: a reference to this package is the registration intent, and the host
/// names nothing but where Initialize runs. What the script carries -- the client key's header, the
/// key itself -- is read from the host's WebMcp configuration row, never from code.
/// </para>
/// <para>
/// ORDERING: Initialize resolves the declared tools against the route table, so it runs after the
/// endpoints are mapped. A host defers it out of the PlatformServices collect and runs it last:
/// </para>
/// <code>
/// PlatformServices.WebMcp.Initialize(app, loggerFactory, defer: true);
/// PlatformServices.Endpoints.Initialize(app, loggerFactory, defer: true);
/// PlatformServices.Initialize(app, loggerFactory);
/// PlatformServices.Endpoints.Initialize(app, loggerFactory);
/// PlatformServices.WebMcp.Initialize(app, loggerFactory);
/// </code>
/// </remarks>
[PlatformServiceProvider(ServiceCategory = "WebMcp")]
public sealed class WebMcpHosting
{
    /// <summary>The path the script is served on.</summary>
    public const string ScriptPath = "/.well-known/webmcp.js";

    /// <summary>Gets or sets the name of the WebMcp configuration row this host reads.</summary>
    /// <remarks>A fixed value a deployment can change rather than one nothing can override.</remarks>
    public static string ConfigurationName { get; set; } = "WebMcp";

    /// <summary>Phase 1a: nothing to configure; the settings are read at Initialize.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Phase 1b: registers the tool registry, the script generator and the settings reader.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        System.ArgumentNullException.ThrowIfNull(builder);
        var registry = new WebMcpToolRegistry();
        builder.Services.TryAddSingleton<IWebMcpToolRegistry>(registry);
        builder.Services.TryAddSingleton(registry);
        builder.Services.TryAddSingleton<WebMcpJsGenerator>();
        builder.Services.TryAddSingleton<WebMcpHostSettingsReader>();
        WebMcpLog.Registered(loggerFactory?.CreateLogger<WebMcpHosting>() ?? (ILogger)NullLogger.Instance);
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2: reads the settings, resolves the declared tools against the route table and maps the
    /// script. Runs after the endpoints are mapped; see the remarks on this type.
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    /// <returns>The host, or a failure naming what stops WebMCP from being served.</returns>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHost>.Success(host);
        }

        System.ArgumentNullException.ThrowIfNull(host);
        ILogger logger = loggerFactory?.CreateLogger<WebMcpHosting>() ?? (ILogger)NullLogger.Instance;
        if (host is not IEndpointRouteBuilder endpoints)
        {
            return GenericResult<IHost>.Failure(WebMcpLog.NoRouteTable(logger, host.GetType().Name));
        }

        var settings = ReadSettings(host.Services, logger);
        if (settings.IsFailure)
        {
            return settings.ToNewResult<IHost>();
        }

        var registry = host.Services.GetRequiredService<WebMcpToolRegistry>();
        // Why the app's own data sources rather than a resolved EndpointDataSource: the composite is
        // not reliably resolvable from the container, and the route table this reads has to be the
        // one this application built -- a tool resolved against any other is a route that will not
        // match.
        registry.Resolve(
            DeclaredWebMcpTools.Declarations,
            endpoints.DataSources.SelectMany(static source => source.Endpoints).ToList(),
            logger);

        var generator = host.Services.GetRequiredService<WebMcpJsGenerator>();
        var resolved = settings.Value!;
        endpoints.MapGet(ScriptPath, (HttpContext httpContext) => Script(httpContext, generator.Generate(resolved)))
            .ExcludeFromDescription();

        WebMcpLog.ScriptMapped(logger, ScriptPath, registry.Tools.Count);
        return GenericResult<IHost>.Success(host);
    }

    /// <summary>The catalogue as one request gets it: in full, or 304 when the copy the caller holds is this one.</summary>
    /// <param name="httpContext">The request.</param>
    /// <param name="script">The catalogue as the generator writes it now.</param>
    /// <returns>The script, or 304 Not Modified.</returns>
    /// <remarks>
    /// Why no-cache with an ETag rather than a max-age: the catalogue is the endpoint registry's, so it changes with every
    /// deploy, and a cache that keeps it (a CDN in front of the host held one for hours) hides a deploy's tools from every
    /// agent. no-cache lets a cache keep it but never answer from it without asking; asking costs a 304. The tag is the
    /// script's own hash, so it changes exactly when the catalogue does. A CDN that compresses the script may weaken the tag
    /// (W/"..."), which still holds it.
    /// </remarks>
    private static IResult Script(HttpContext httpContext, string script)
    {
        var tag = string.Concat("\"", System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(script))), "\"");
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.ETag = tag;

        return httpContext.Request.Headers.IfNoneMatch.ToString().Contains(tag, System.StringComparison.Ordinal)
            ? Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status304NotModified)
            : Microsoft.AspNetCore.Http.Results.Content(script, "application/javascript");
    }

    /// <summary>The host's WebMCP settings, read through the reader this type registered.</summary>
    /// <param name="services">The built host's services.</param>
    /// <param name="logger">Receives a message when a service the read needs is missing.</param>
    /// <returns>The settings, or a failure naming what is missing.</returns>
    /// <remarks>
    /// Shared with the MCP bridge's registrant, which serves the same settings over a second transport.
    /// </remarks>
    public static IGenericResult<WebMcpHostSettings> ReadSettings(System.IServiceProvider services, ILogger logger)
    {
        System.ArgumentNullException.ThrowIfNull(services);
        if (services.GetService<IWebMcpConfigurationProvider>() is null)
        {
            return GenericResult<WebMcpHostSettings>.Failure(WebMcpLog.NotRegistered(logger, nameof(IWebMcpConfigurationProvider)));
        }

        if (services.GetService<ISecretManagerProvider>() is null)
        {
            return GenericResult<WebMcpHostSettings>.Failure(WebMcpLog.NotRegistered(logger, nameof(ISecretManagerProvider)));
        }

        // Why blocking: Initialize is synchronous and runs once, before the host takes a request.
#pragma warning disable VSTHRD002
        return services.GetRequiredService<WebMcpHostSettingsReader>().Read().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }
}
