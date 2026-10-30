using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Watches the runs that wait and acts on the thresholds on them (FDW-3161). The scheduler tells it each time the waiting runs change; it counts them, judges every
/// threshold the host is configured with, and sends the notification rules of each threshold that begins to fire. It also answers whether a run that arrives is taken:
/// not while a threshold that stops new runs fires and applies to the run's schedule and pipeline.
/// </summary>
/// <remarks>
/// Why a background loop reads the changes: the scheduler calls the observer on the path that queues or ends a run, which must stay fast; only the latest
/// list matters, so a change that arrives while the last one is judged replaces any change waiting behind it.
/// </remarks>
public sealed class WaitThresholdMonitor : BackgroundService, IPipelineWaitObserver, IPipelineRunStopCheck
{
    private readonly IWaitThresholdSource _source;
    private readonly IServiceScopeFactory _scopes;
    private readonly WaitThresholdJudge _judge;
    private readonly ILogger _logger;
    private volatile WaitCounts _counts = new(0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
    private readonly Channel<PipelineRunQueueSnapshot> _changes =
        Channel.CreateBounded<PipelineRunQueueSnapshot>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdMonitor"/> class.</summary>
    /// <param name="source">Reads the thresholds the host is configured with.</param>
    /// <param name="scopes">Opens the scope the notification rules are sent in.</param>
    /// <param name="judge">Judges the thresholds and remembers which fire.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdMonitor(IWaitThresholdSource source, IServiceScopeFactory scopes, WaitThresholdJudge judge, ILogger<WaitThresholdMonitor>? logger = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _judge = judge ?? throw new ArgumentNullException(nameof(judge));
        _logger = logger ?? NullLogger<WaitThresholdMonitor>.Instance;
    }

    /// <inheritdoc/>
    public void WaitingChanged(PipelineRunQueueSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _counts = Count(snapshot);
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

        var effective = WaitThresholdResolver.Effective(configuration.Value.Server, pipeline, schedule, _logger);
        if (effective.IsFailure || effective.Value is null)
        {
            return effective.ToNewResult<PipelineRunStopCheck>();
        }

        var stopping = WaitThresholdJudge.Stopping(effective.Value, _counts);
        if (stopping is null)
        {
            return GenericResult<PipelineRunStopCheck>.Success(PipelineRunStopCheck.Open);
        }

        WaitThresholdLog.RunStopped(_logger, request.PipelineName, stopping.Name, stopping.Owner);
        return GenericResult<PipelineRunStopCheck>.Success(new PipelineRunStopCheck(
            true, stopping.Name, $"The server is not taking new runs: the threshold '{stopping.Name}' of {stopping.Owner} on waiting runs fires"));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var snapshot in _changes.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await Evaluate(snapshot, stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>Judges every threshold against the runs that wait and sends the notification rules of those that begin to fire.</summary>
    /// <param name="snapshot">The list of runs as it stands.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that ends when the thresholds are judged and the rules sent.</returns>
    public async Task Evaluate(PipelineRunQueueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var configuration = await _source.Read(cancellationToken).ConfigureAwait(false);
        if (configuration.IsFailure || configuration.Value is null)
        {
            WaitThresholdLog.ConfigurationNotRead(_logger, configuration.CurrentMessage);
            return;
        }

        var counts = Count(snapshot);
        var change = _judge.Judge(configuration.Value.All(), counts);
        foreach (var threshold in change.Fired)
        {
            await Notify(threshold, counts.Of(threshold.CountedPipeline), cancellationToken).ConfigureAwait(false);
        }
    }

    private static WaitCounts Count(PipelineRunQueueSnapshot snapshot) =>
        new(
            snapshot.Waiting.Count,
            snapshot.Waiting.GroupBy(entry => entry.Run.PipelineName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase));

    private async Task Notify(WaitThreshold threshold, int count, CancellationToken cancellationToken)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            using (new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>()))
            {
                var notifier = scope.ServiceProvider.GetRequiredService<IRuleNotifier>();
                foreach (var rule in threshold.NotificationRules)
                {
                    var sent = await notifier.Notify(
                        new RuleNotification
                        {
                            RuleName = rule,
                            Subject = $"Waiting runs: {threshold.Name}",
                            Message = $"The threshold '{threshold.Name}' of {threshold.Owner} fires: {count} waiting runs, {threshold.Comparison.Name} {threshold.Number}",
                            Metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                            {
                                ["Threshold"] = threshold.Name,
                                ["Owner"] = threshold.Owner,
                                ["WaitingRuns"] = count,
                                ["Severity"] = threshold.Severity.Name,
                                ["StopsNewRuns"] = threshold.StopsNewRuns,
                            },
                        },
                        cancellationToken).ConfigureAwait(false);
                    if (sent.IsFailure)
                    {
                        WaitThresholdLog.NotificationNotSent(_logger, threshold.Name, rule, sent.CurrentMessage);
                    }
                }
            }
        }
    }
}
