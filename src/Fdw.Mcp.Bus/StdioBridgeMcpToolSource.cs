using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Mcp.Bus.Abstractions;
using Fdw.Mcp.Bus.Results;
using Fdw.Results;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Stdio-bridge tool source — spawns an external MCP server process and bridges its JSON-RPC
/// stdio to the in-process <see cref="IMcpEventBus"/>. Bus <c>invoke</c> events become
/// JSON-RPC <c>tools/call</c> requests; JSON-RPC responses become bus <c>result</c> / <c>error</c>
/// events with the matching CorrelationId.
/// </summary>
/// <remarks>
/// The child process is initialized with the MCP <c>initialize</c> + <c>initialized</c>
/// handshake before any tool calls are forwarded.
/// </remarks>
public sealed class StdioBridgeMcpToolSource : IMcpToolSource
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<StdioBridgeMcpToolSource> _logger;
    private readonly string _command;
    private readonly IReadOnlyList<string> _arguments;
    private readonly ConcurrentDictionary<long, PendingCall> _pending = new();
    private CancellationTokenSource? _cts;
    private Task? _busPump;
    private Task? _stdoutPump;
    private Process? _process;
    private long _nextRpcId = 1;

    /// <inheritdoc />
    public string ServerName { get; }

    /// <summary>Initializes a stdio-bridge tool source.</summary>
    public StdioBridgeMcpToolSource(
        string serverName,
        string command,
        IReadOnlyList<string>? arguments = null,
        ILogger<StdioBridgeMcpToolSource>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ServerName = serverName;
        _command = command;
        _arguments = arguments ?? Array.Empty<string>();
        _logger = logger ?? NullLogger<StdioBridgeMcpToolSource>.Instance;
    }

    /// <inheritdoc />
    public async Task Start(IMcpEventBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (_process is not null) return;

        var psi = new ProcessStartInfo(_command)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in _arguments) psi.ArgumentList.Add(a);

        _process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {_command}");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var invokeStream = bus.Subscribe($"mcp/{ServerName}/*/invoke", _cts.Token);
        _stdoutPump = Task.Run(() => PumpStdout(_process.StandardOutput, _cts.Token), _cts.Token);
        _busPump = Task.Run(() => PumpBus(bus, invokeStream, _cts.Token), _cts.Token);

        await SendRpc(new { jsonrpc = "2.0", id = NextId(), method = "initialize", @params = new { protocolVersion = "2024-11-05", clientInfo = new { name = "Fdw.Mcp.Bus", version = "0.1" }, capabilities = new { } } }, _cts.Token).ConfigureAwait(false);
        await SendRpc(new { jsonrpc = "2.0", method = "notifications/initialized" }, _cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Stop(CancellationToken cancellationToken)
    {
        if (_cts is null) return;
        await _cts.CancelAsync().ConfigureAwait(false);

#pragma warning disable VSTHRD003
        if (_busPump is not null) { try { await _busPump.ConfigureAwait(false); } catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested) { } }
        if (_stdoutPump is not null) { try { await _stdoutPump.ConfigureAwait(false); } catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested) { } }
#pragma warning restore VSTHRD003

        if (_process is { HasExited: false })
        {
            try { _process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException ex) { McpBusLog.ProcessKillRaceCondition(_logger, ex); }
        }
        _process?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Stop(CancellationToken.None).ConfigureAwait(false);
        _cts?.Dispose();
    }

    private async Task PumpBus(IMcpEventBus bus, IAsyncEnumerable<McpEvent> stream, CancellationToken ct)
    {
        await foreach (var evt in stream.ConfigureAwait(false))
        {
            var toolName = ExtractToolName(evt.Topic);
            if (toolName is null) continue;

            var rpcId = NextId();
            var tcs = new TaskCompletionSource<IGenericResult<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[rpcId] = new PendingCall(toolName, evt, tcs);

            try
            {
                using var argsDoc = JsonDocument.Parse(evt.Payload);
                var request = new
                {
                    jsonrpc = "2.0",
                    id = rpcId,
                    method = "tools/call",
                    @params = new { name = toolName, arguments = argsDoc.RootElement },
                };
                await SendRpc(request, ct).ConfigureAwait(false);

                var response = await tcs.Task.WaitAsync(ct).ConfigureAwait(false);

                // The tool answering with an error is an outcome, not an exception: republish it as a
                // bus error event and carry on pumping. Only genuine faults reach the catch below.
                if (!response.IsSuccess)
                {
                    await bus.PublishToolError(ServerName, toolName, response.CurrentMessage ?? response.Code!.Name,
                        evt.CorrelationId, evt.EventId, ct).ConfigureAwait(false);
                    continue;
                }

                await bus.PublishToolResult(ServerName, toolName, response.Value, evt.View, evt.CorrelationId, evt.EventId, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested && ex.CancellationToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                McpBusLog.StdioBridgeFailed(_logger, ServerName, toolName, ex);
                try { await bus.PublishToolError(ServerName, toolName, ex.Message, evt.CorrelationId, evt.EventId, ct).ConfigureAwait(false); }
                catch (OperationCanceledException ex2) when (ct.IsCancellationRequested && ex2.CancellationToken.IsCancellationRequested) { return; }
            }
            finally
            {
                _pending.TryRemove(rpcId, out _);
            }
        }
    }

    private async Task PumpStdout(StreamReader stdout, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? line;
            try { line = await stdout.ReadLineAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested && ex.CancellationToken.IsCancellationRequested) { return; }
            if (line is null) return;
            if (line.Length == 0) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                if (!doc.RootElement.TryGetProperty("id", out var idElement)) continue;
                if (idElement.ValueKind != JsonValueKind.Number) continue;

                var id = idElement.GetInt64();
                if (!_pending.TryGetValue(id, out var pending)) continue;

                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    var message = error.TryGetProperty("message", out var m) ? (m.GetString() ?? "tool error") : "tool error";
                    pending.Completion.TrySetResult(GenericResult<JsonElement>.Failure(
                        McpBusResultCodes.ByName("ToolReportedError"), _logger,
                        ResultDetails.Create("server", ServerName, "tool", pending.ToolName, "error", message)
                            .With("correlationId", pending.Invoke.CorrelationId)));
                }
                else if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    pending.Completion.TrySetResult(GenericResult<JsonElement>.Success(result.Clone()));
                }
            }
            catch (JsonException ex)
            {
                McpBusLog.StdioBridgeNonJsonDiscarded(_logger, ServerName, line, ex);
            }
        }
    }

    private async Task SendRpc(object payload, CancellationToken ct)
    {
        if (_process is null) throw new InvalidOperationException("Process not started.");
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    private long NextId() => Interlocked.Increment(ref _nextRpcId);

    private static string? ExtractToolName(string topic)
    {
        var parts = topic.Split('/');
        return parts.Length == 4 ? parts[2] : null;
    }

    private sealed record PendingCall(string ToolName, McpEvent Invoke, TaskCompletionSource<IGenericResult<JsonElement>> Completion);
}
