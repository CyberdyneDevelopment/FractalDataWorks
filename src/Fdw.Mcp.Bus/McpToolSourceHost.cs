using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Hosted service that owns the lifetime of every registered <see cref="IMcpToolSource"/>:
/// starts them at app startup, stops them at shutdown.
/// </summary>
/// <remarks>
/// Use one of the <c>AddInProcMcpToolSource</c> / <c>AddStdioBridgeMcpToolSource</c> DI
/// extensions to register sources; this host service picks them all up by enumerating
/// <c>IEnumerable&lt;IMcpToolSource&gt;</c>.
/// </remarks>
public sealed class McpToolSourceHost : IHostedService, IAsyncDisposable
{
    private readonly IMcpEventBus _bus;
    private readonly IReadOnlyList<IMcpToolSource> _sources;
    private readonly ILogger<McpToolSourceHost> _logger;

    /// <summary>Initializes the tool-source host.</summary>
    public McpToolSourceHost(IMcpEventBus bus, IEnumerable<IMcpToolSource> sources, ILogger<McpToolSourceHost>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(sources);
        _bus = bus;
        _sources = new List<IMcpToolSource>(sources);
        _logger = logger ?? NullLogger<McpToolSourceHost>.Instance;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var source in _sources)
        {
            McpBusLog.ToolSourceStarting(_logger, source.ServerName);
            await source.Start(_bus, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var source in _sources)
        {
            try { await source.Stop(cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) { McpBusLog.ToolSourceStopThrew(_logger, source.ServerName, ex); }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var source in _sources)
        {
            try { await source.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex) { McpBusLog.ToolSourceDisposeThrew(_logger, source.ServerName, ex); }
        }
    }
}
