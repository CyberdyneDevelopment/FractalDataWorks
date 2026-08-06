using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// In-process tool source — the caller supplies a delegate that handles tool invocations
/// synchronously inside the host process. The MCP tool implementation is a library reference;
/// no external process is involved.
/// </summary>
/// <remarks>
/// Use when the host can directly host the MCP tool service (e.g. pidgin hosts FDW's
/// <c>SqlToolService</c> via library reference). Simpler than the stdio bridge but ties the
/// host's dependency graph to the tool's.
/// </remarks>
public sealed class InProcMcpToolSource : IMcpToolSource
{
    private readonly ILogger<InProcMcpToolSource> _logger;
    private readonly Func<string, JsonElement, CancellationToken, Task<object>> _handler;
    private CancellationTokenSource? _cts;
    private Task? _pump;

    /// <inheritdoc />
    public string ServerName { get; }

    /// <summary>Initializes an in-proc tool source.</summary>
    /// <param name="serverName">MCP server name this source provides (e.g. <c>"mssql"</c>).</param>
    /// <param name="handler">Per-invocation handler: receives tool name and args JSON, returns the payload.</param>
    /// <param name="logger">Optional logger.</param>
    public InProcMcpToolSource(
        string serverName,
        Func<string, JsonElement, CancellationToken, Task<object>> handler,
        ILogger<InProcMcpToolSource>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentNullException.ThrowIfNull(handler);
        ServerName = serverName;
        _handler = handler;
        _logger = logger ?? NullLogger<InProcMcpToolSource>.Instance;
    }

    /// <inheritdoc />
    public Task Start(IMcpEventBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (_pump is not null) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _cts.Token;
        // Why: call Subscribe synchronously on the caller's thread so the subscription is live
        // before Start returns. The pump then drains the already-live channel on the thread pool.
        var stream = bus.Subscribe($"mcp/{ServerName}/*/invoke", ct);
        _pump = Task.Run(() => Pump(bus, stream, ct), ct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task Stop(CancellationToken cancellationToken)
    {
        if (_cts is null) return;
        await _cts.CancelAsync().ConfigureAwait(false);
        if (_pump is not null)
        {
#pragma warning disable VSTHRD003
            try { await _pump.ConfigureAwait(false); } catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested) { }
#pragma warning restore VSTHRD003
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Stop(CancellationToken.None).ConfigureAwait(false);
        _cts?.Dispose();
    }

    private async Task Pump(IMcpEventBus bus, IAsyncEnumerable<McpEvent> stream, CancellationToken ct)
    {
        await foreach (var evt in stream.ConfigureAwait(false))
        {
            var toolName = ExtractToolName(evt.Topic);
            if (toolName is null) continue;

            try
            {
                var argsDoc = JsonDocument.Parse(evt.Payload);
                var result = await _handler(toolName, argsDoc.RootElement, ct).ConfigureAwait(false);
                await bus.PublishToolResult(ServerName, toolName, result, evt.View, evt.CorrelationId, evt.EventId, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested && ex.CancellationToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                McpBusLog.InProcToolSourceThrew(_logger, ServerName, toolName, ex);
                try
                {
                    await bus.PublishToolError(ServerName, toolName, ex.Message, evt.CorrelationId, evt.EventId, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex2) when (ct.IsCancellationRequested && ex2.CancellationToken.IsCancellationRequested) { return; }
            }
        }
    }

    private static string? ExtractToolName(string topic)
    {
        // Topic shape: mcp/{server}/{tool}/invoke
        var parts = topic.Split('/');
        return parts.Length == 4 ? parts[2] : null;
    }
}
