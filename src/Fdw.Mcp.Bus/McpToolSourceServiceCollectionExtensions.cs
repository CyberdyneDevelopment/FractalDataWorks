using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>DI registration helpers for MCP tool sources.</summary>
public static class McpToolSourceServiceCollectionExtensions
{
    /// <summary>
    /// Register an in-process tool source for <paramref name="serverName"/>. Each invocation of
    /// any <c>mcp/{serverName}/{tool}/invoke</c> event is dispatched to <paramref name="handler"/>;
    /// its return value becomes the result event.
    /// </summary>
    public static IServiceCollection AddInProcMcpToolSource(
        this IServiceCollection services,
        string serverName,
        Func<string, JsonElement, CancellationToken, Task<object>> handler)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);

        EnsureToolSourceHost(services);
        services.AddSingleton<IMcpToolSource>(sp =>
            new InProcMcpToolSource(serverName, handler, sp.GetService<ILogger<InProcMcpToolSource>>()));
        return services;
    }

    /// <summary>
    /// Register a stdio-bridge tool source. The bridge spawns <paramref name="command"/>
    /// (optionally with <paramref name="arguments"/>) at startup, runs the MCP initialize
    /// handshake, and forwards bus invocations as JSON-RPC tool calls.
    /// </summary>
    public static IServiceCollection AddStdioBridgeMcpToolSource(
        this IServiceCollection services,
        string serverName,
        string command,
        IReadOnlyList<string>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        EnsureToolSourceHost(services);
        services.AddSingleton<IMcpToolSource>(sp =>
            new StdioBridgeMcpToolSource(serverName, command, arguments, sp.GetService<ILogger<StdioBridgeMcpToolSource>>()));
        return services;
    }

    private static void EnsureToolSourceHost(IServiceCollection services)
    {
        // Why: TryAddEnumerable avoids registering the host twice if the caller chains multiple
        // AddInProc / AddStdioBridge calls. One host serves all registered sources.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, McpToolSourceHost>());
    }
}
