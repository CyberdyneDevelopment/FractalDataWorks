using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// Serves this host's WebMCP tool catalogue to headless agents over MCP: registers an MCP server over
/// HTTP and maps it at <see cref="McpPath"/>.
/// </summary>
/// <remarks>
/// <para>
/// WebMCP tools are registered through <c>document.modelContext</c>, so they exist only inside a
/// browser page. An agent without a page -- a terminal session, a background job -- cannot call one.
/// This maps the same catalogue onto MCP over HTTP, so both kinds of agent drive the same endpoints.
/// ONE catalogue, two transports: the tool list and every tool's schema come from the registry
/// <see cref="WebMcpHosting"/> fills, which is what <c>webmcp.js</c> is rendered from.
/// </para>
/// <para>
/// A tool call goes to the tool's own route at the host's <c>SelfBaseAddress</c>, carrying the
/// calling agent's <c>Authorization</c> header, so authentication and authorization are whatever that
/// route already does. The address is read from the host's WebMcp configuration row; a host without
/// one is refused at Initialize rather than failing on its first tool call.
/// </para>
/// <para>
/// ORDERING: deferred and run last, after <see cref="WebMcpHosting"/> -- see the remarks there.
/// </para>
/// </remarks>
[PlatformServiceProvider(ServiceCategory = "WebMcpMcp")]
public sealed class WebMcpMcpHosting
{
    /// <summary>The path the MCP endpoint is served on.</summary>
    public const string McpPath = "/mcp";

    /// <summary>Phase 1a: nothing to configure; the settings are read at Initialize.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Phase 1b: registers the MCP server, its catalogue and its tool invoker.</summary>
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

        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;
        services.TryAddSingleton<WebMcpMcpCatalogue>();
        services.TryAddSingleton<WebMcpToolInvoker>();
        // Needed to forward the calling agent's identity to the tool's own route.
        services.AddHttpContextAccessor();
        services.AddHttpClient(WebMcpToolInvoker.ClientName);
        services.AddMcpServer()
            .WithHttpTransport()
            // Handlers rather than pre-built tools: the registry is resolved against the route table
            // at WebMcpHosting's Initialize, which is after registration, so a tool list captured here
            // would always be empty.
            .WithListToolsHandler((context, _) =>
                ValueTask.FromResult(new ListToolsResult
                {
                    Tools = context.Services!.GetRequiredService<WebMcpMcpCatalogue>().Tools(),
                }))
            .WithCallToolHandler(async (context, cancellationToken) =>
                await context.Services!.GetRequiredService<WebMcpToolInvoker>()
                    .Invoke(context.Params!.Name, (IReadOnlyDictionary<string, JsonElement>?)context.Params.Arguments, cancellationToken)
                    .ConfigureAwait(false));

        McpBridgeLog.Registered(loggerFactory?.CreateLogger<WebMcpMcpHosting>() ?? (ILogger)NullLogger.Instance);
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2: checks the host can reach its own routes, then maps the MCP endpoint. Runs after
    /// the endpoints are mapped; see the remarks on this type.
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    /// <returns>The host, or a failure naming what stops the endpoint from being served.</returns>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHost>.Success(host);
        }

        ArgumentNullException.ThrowIfNull(host);
        ILogger logger = loggerFactory?.CreateLogger<WebMcpMcpHosting>() ?? (ILogger)NullLogger.Instance;
        if (host is not IEndpointRouteBuilder endpoints)
        {
            return GenericResult<IHost>.Failure(McpBridgeLog.NoRouteTable(logger, host.GetType().Name));
        }

        var settings = WebMcpHosting.ReadSettings(host.Services, logger);
        if (settings.IsFailure)
        {
            return settings.ToNewResult<IHost>();
        }

        // Refused here rather than on the first tool call: a bridge that cannot reach its own routes
        // would accept the call and have nowhere to send it.
        if (settings.Value!.SelfBaseAddress is not { } self)
        {
            return GenericResult<IHost>.Failure(McpBridgeLog.SelfBaseAddressMissing(logger));
        }

        endpoints.MapMcp(McpPath);
        McpBridgeLog.Mapped(logger, McpPath, self.ToString());
        return GenericResult<IHost>.Success(host);
    }
}
