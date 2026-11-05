using System;
using Fdw.Collections;
using Fdw.Conventions;
using Fdw.DevSession.Mcp;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.DevSession;

/// <summary>
/// Registers the development-session domain and its stdio MCP server through the PlatformServices phases
/// (FDW-3957), so the Fdw.DevSession.McpServer executable registers nothing itself (FDW056).
/// </summary>
[PlatformServiceProvider(ServiceCategory = "DevSession")]
public sealed class DevSessionHosting
{
    /// <summary>How many events the session event bus keeps for late subscribers, unless a host states another (FDW-3925).</summary>
    [PlatformDefault]
    public const int DefaultRingCapacity = 10_000;

    /// <summary>Configure phase: nothing to bind.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The builder.</returns>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Register phase: the session domain, its event bus, and the stdio MCP server with the session tools.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The builder.</returns>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddDevSessions(bus => bus.RingCapacity = DefaultRingCapacity);
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<DevSessionToolService>();

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>Initialize phase: nothing to start; the MCP server runs as a hosted service.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The host.</returns>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHost>.Success(host);
}
