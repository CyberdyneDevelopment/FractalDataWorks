using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Channel-based bounded queue for pipeline execution requests.
/// Provides backpressure by returning false when the queue is full.
/// </summary>
/// <remarks>
/// Why the full mode is Wait: the channel writer's TryWrite answers false for a full channel only in that mode. In DropWrite mode it
/// discards the new item and answers true, so the caller is told a trigger was queued that never was and no trigger endpoint ever reaches
/// its queue-full answer. Nothing here waits: Enqueue only calls TryWrite.
/// <para>
/// Why the request is recorded here (FDW-3216): every way a run is accepted ends in this one call, so a run the host takes up again after a restart
/// has its request on its record whichever way it came. The request is recorded before it is taken, so a run is never in the queue without a record of it.
/// </para>
/// </remarks>
public sealed class PipelineExecutionQueue : IPipelineExecutionQueue
{
    private readonly Channel<PipelineExecutionRequest> _channel;
    private readonly int _capacity;
    private readonly IPipelineRunRequestStore _store;
    private readonly IPipelineRunStopCheck _stops;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionQueue"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">
    /// Maximum number of pending execution requests: the setting <see cref="SettingDefinitions.MaxQueuedPipelineTriggers"/> (FDW-3155).
    /// It bounds memory against request storms. There is no default; it is at least one.
    /// </param>
    /// <param name="store">Records the request of each run the queue takes.</param>
    /// <param name="stops">Says whether a threshold on waiting runs that stops new runs fires for a run that arrives (FDW-3161).</param>
    /// <param name="logger">The logger a dropped trigger is written to.</param>
    public PipelineExecutionQueue(int capacity, IPipelineRunRequestStore store, IPipelineRunStopCheck stops, ILogger<PipelineExecutionQueue>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _stops = stops ?? throw new ArgumentNullException(nameof(stops));
        _logger = logger ?? NullLogger<PipelineExecutionQueue>.Instance;
        _channel = Channel.CreateBounded<PipelineExecutionRequest>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A request that cannot be recorded throws: answering that the queue is full would tell the caller something that is not why the run was not taken. So does a
    /// threshold configuration that cannot be read: a server whose thresholds are stated wrongly does not take runs it cannot judge.
    /// </remarks>
    public async ValueTask<PipelineEnqueueResult> Enqueue(
        PipelineExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var stop = await _stops.Check(request, cancellationToken).ConfigureAwait(false);
        if (stop.IsFailure || stop.Value is null)
        {
            throw new InvalidOperationException(string.Concat(stop.CurrentMessage));
        }

        if (stop.Value.IsStopped)
        {
            return PipelineEnqueueResult.Stopped(stop.Value.Reason);
        }

        var recorded = await _store.Record(request, cancellationToken).ConfigureAwait(false);
        if (recorded.IsFailure)
        {
            throw new InvalidOperationException(string.Concat(recorded.CurrentMessage));
        }

        if (_channel.Writer.TryWrite(request))
        {
            return PipelineEnqueueResult.Accepted;
        }

        PipelineRunSchedulerLog.TriggerDropped(_logger, _capacity, SettingDefinitions.MaxQueuedPipelineTriggers, request.PipelineName);
        return PipelineEnqueueResult.Full($"The queue of pipeline triggers holds {_capacity} and is full");
    }

    /// <summary>
    /// Builds the queue the host starts with: the number is the setting <see cref="SettingDefinitions.MaxQueuedPipelineTriggers"/>, read once
    /// for the host under the system context, since no request is in flight when the host starts.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The queue.</returns>
    /// <exception cref="InvalidOperationException">The setting is missing or not positive; the host does not start without it.</exception>
    public static PipelineExecutionQueue Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<PipelineExecutionQueue>();
        using var scope = services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>();
        if (accessor.Current is null)
        {
            accessor.Current = new SystemAuthenticationContext();
        }

        var capacity = PipelineTriggerQueueLimit.Read(scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>(), logger);
        return capacity.IsSuccess
            ? new PipelineExecutionQueue(
                capacity.Value, scope.ServiceProvider.GetRequiredService<IPipelineRunRequestStore>(), scope.ServiceProvider.GetRequiredService<IPipelineRunStopCheck>(), logger)
            : throw new InvalidOperationException(string.Concat(capacity.CurrentMessage));
    }

    /// <summary>
    /// Gets the channel reader for the background service to consume from.
    /// Why internal: only PipelineExecutionBackgroundService (same assembly) reads from the
    /// channel. Endpoints should never read from the queue directly.
    /// </summary>
    internal ChannelReader<PipelineExecutionRequest> Reader => _channel.Reader;
}
