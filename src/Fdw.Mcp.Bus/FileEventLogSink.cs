using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Subscribes to every event on the bus and appends it to an hourly-rotating JSON Lines file.
/// Provides observability and a future replay-beyond-ring source; replay integration lands with
/// Wave 5.
/// </summary>
public sealed class FileEventLogSink : IHostedService, IAsyncDisposable
{
    private readonly IMcpEventBus _bus;
    private readonly McpEventBusOptions _options;
    private readonly ILogger<FileEventLogSink> _logger;
    private CancellationTokenSource? _cts;
    private Task? _pump;

    /// <summary>Initializes the file event log sink.</summary>
    public FileEventLogSink(IMcpEventBus bus, IOptions<McpEventBusOptions> options, ILogger<FileEventLogSink>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(options);
        _bus = bus;
        _options = options.Value;
        _logger = logger ?? NullLogger<FileEventLogSink>.Instance;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FileLogDirectory))
        {
            McpBusLog.FileSinkDisabled(_logger);
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(_options.FileLogDirectory);
        _cts = new CancellationTokenSource();
        var dir = _options.FileLogDirectory;
        var ct = _cts.Token;
        _pump = Task.Run(() => Pump(dir, ct), ct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null) return;
        await _cts.CancelAsync().ConfigureAwait(false);
        if (_pump is not null)
        {
            // Standard IHostedService pattern: await the long-lived pump we started in StartAsync.
#pragma warning disable VSTHRD003
            try { await _pump.ConfigureAwait(false); } catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested) { }
#pragma warning restore VSTHRD003
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _cts?.Dispose();
    }

    private async Task Pump(string dir, CancellationToken ct)
    {
        await foreach (var evt in _bus.Subscribe("**", ct).ConfigureAwait(false))
        {
            try
            {
                var path = Path.Combine(dir, Segment(evt.Timestamp));
                var line = Serialize(evt);
                await File.AppendAllTextAsync(path, line + Environment.NewLine, Encoding.UTF8, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                McpBusLog.FileAppendFailed(_logger, evt.EventId, ex);
            }
        }
    }

    private static string Segment(DateTimeOffset ts) =>
        $"bus-{ts.UtcDateTime.ToString("yyyyMMddHH", CultureInfo.InvariantCulture)}.jsonl";

    private static string Serialize(McpEvent evt)
    {
        var payload = new
        {
            eventId = evt.EventId,
            topic = evt.Topic,
            ts = evt.Timestamp,
            corr = evt.CorrelationId,
            caus = evt.Causation,
            view = evt.View.Name,
            payloadType = evt.PayloadType,
            payload = Convert.ToBase64String(evt.Payload.Span),
        };
        return JsonSerializer.Serialize(payload);
    }
}
