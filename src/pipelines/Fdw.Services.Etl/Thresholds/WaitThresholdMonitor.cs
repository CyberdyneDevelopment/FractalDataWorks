using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Watches the runs that wait and hands them to the one threshold evaluator (FDW-3161, FDW-3975). The scheduler tells it each time the waiting runs change; it tells the
/// <see cref="WaitThresholdReporter"/>, which counts them for each pipeline, schedule and the server and has the evaluator judge the thresholds attached to them. It also
/// answers whether a run that arrives is taken: the levels choose the owner whose thresholds apply and the <see cref="WaitHoldGate"/> asks which HoldNewRuns rules hold.
/// </summary>
/// <remarks>
/// Why a background loop reads the changes: the scheduler calls the observer on the path that queues or ends a run, which must stay fast; only the latest
/// list matters, so a change that arrives while the last one is judged replaces any change waiting behind it.
/// </remarks>
public sealed class WaitThresholdMonitor : BackgroundService, IPipelineWaitObserver, IPipelineRunStopCheck
{
    private readonly IWaitThresholdSource _source;
    private readonly WaitHoldGate _gate;
    private readonly WaitThresholdReporter _reporter;
    private readonly ILogger _logger;
    private volatile PipelineRunQueueSnapshot _snapshot = new([], []);
    private readonly Channel<PipelineRunQueueSnapshot> _changes =
        Channel.CreateBounded<PipelineRunQueueSnapshot>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdMonitor"/> class.</summary>
    /// <param name="source">Reads the level each owner states.</param>
    /// <param name="gate">Says whether a run that arrives is held.</param>
    /// <param name="reporter">Tells the threshold evaluator how many runs wait.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdMonitor(IWaitThresholdSource source, WaitHoldGate gate, WaitThresholdReporter reporter, ILogger<WaitThresholdMonitor>? logger = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _logger = logger ?? NullLogger<WaitThresholdMonitor>.Instance;
    }

    /// <inheritdoc/>
    public void WaitingChanged(PipelineRunQueueSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
        _ = _changes.Writer.TryWrite(snapshot);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<PipelineRunStopCheck>> Check(PipelineExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var configuration = await _source.Read(cancellationToken).ConfigureAwait(false);
        if (configuration.IsFailure || configuration.Value is null)
        {
            return configuration.ToNewResult<PipelineRunStopCheck>();
        }

        if (!configuration.Value.Pipelines.TryGetValue(request.PipelineName, out var pipeline))
        {
            return GenericResult<PipelineRunStopCheck>.Failure(WaitThresholdLog.PipelineHasNoLevel(_logger, request.PipelineName));
        }

        WaitThresholdLayer? schedule = null;
        if (request.ScheduleName is { } scheduleName && !configuration.Value.Schedules.TryGetValue(scheduleName, out schedule))
        {
            return GenericResult<PipelineRunStopCheck>.Failure(WaitThresholdLog.ScheduleHasNoLevel(_logger, scheduleName));
        }

        var owner = WaitThresholdResolver.Owner(configuration.Value.Server, pipeline, schedule, _logger);
        if (owner.IsFailure || owner.Value is null)
        {
            return owner.ToNewResult<PipelineRunStopCheck>();
        }

        return await _gate.Check(request, owner.Value, _snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var snapshot in _changes.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await Evaluate(snapshot, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Hands the waiting runs in a snapshot to the threshold evaluator.</summary>
    /// <param name="snapshot">The runs in flight and waiting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that ends when the snapshot has been reported.</returns>
    public Task Evaluate(PipelineRunQueueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return _reporter.Report(snapshot, cancellationToken);
    }
}
