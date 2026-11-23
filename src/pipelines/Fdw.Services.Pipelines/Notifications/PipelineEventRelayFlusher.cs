using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Sends the events a host that serves no hub has gathered, once a tick, in one request (FDW-5249). Without it every event is its own request,
/// and N runs in flight cost N requests a tick.
/// </summary>
/// <remarks>
/// Each tick takes everything in the <see cref="PipelineEventRelayBuffer"/> and sends it, oldest first, through <see cref="IPipelineEventBatchClient"/>.
/// A tick with nothing waiting sends nothing. A send that fails is one coded failure for the tick and is not retried: the next tick carries
/// the next figures, and a run never fails because a browser could not be told.
/// </remarks>
public sealed class PipelineEventRelayFlusher : BackgroundService
{
    private readonly PipelineEventRelayBuffer _buffer;
    private readonly PipelineEventRelayTuning _tuning;
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostEnvironment _environment;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineEventRelayFlusher"/> class.</summary>
    /// <param name="buffer">The host's buffer.</param>
    /// <param name="tuning">How often to send.</param>
    /// <param name="scopes">Opens the scope the batch client lives in.</param>
    /// <param name="environment">Names this host, for the message when it has no client.</param>
    /// <param name="logger">The logger.</param>
    public PipelineEventRelayFlusher(
        PipelineEventRelayBuffer buffer,
        PipelineEventRelayTuning tuning,
        IServiceScopeFactory scopes,
        IHostEnvironment environment,
        ILogger<PipelineEventRelayFlusher>? logger = null)
    {
        _buffer = buffer;
        _tuning = tuning;
        _scopes = scopes;
        _environment = environment;
        _logger = logger ?? NullLogger<PipelineEventRelayFlusher>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PipelineEventRelayLog.RelayFlusherStarted(_logger, (int)_tuning.FlushEvery.TotalMilliseconds, _tuning.BufferCapacity);
        using var timer = new PeriodicTimer(_tuning.FlushEvery);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await FlushOnce(stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Sends what is waiting in the buffer as one batch; does nothing when nothing is waiting.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the batch has been sent or has failed.</returns>
    public async Task FlushOnce(CancellationToken cancellationToken = default)
    {
        var events = _buffer.Drain();
        if (events.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopes.CreateScope();
            if (scope.ServiceProvider.GetService<IPipelineEventBatchClient>() is not { } client)
            {
                PipelineEventRelayLog.RelayClientMissing(_logger, _environment.ApplicationName);
                return;
            }

            var result = await client.RelayBatch(new PipelineEventBatchRequest { Events = [.. events] }, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                PipelineEventRelayLog.BatchRelayFailed(_logger, events.Count);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            PipelineEventRelayLog.BatchRelayException(_logger, exception, events.Count);
        }
    }
}
