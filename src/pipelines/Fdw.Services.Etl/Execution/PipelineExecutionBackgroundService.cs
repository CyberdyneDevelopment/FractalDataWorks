﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Conventions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Tracking;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Background service that dequeues pipeline execution requests and executes them in scoped
/// DI containers, providing correct service lifetimes and graceful shutdown support.
/// </summary>
/// <remarks>
/// Why BackgroundService with Channel: replaces fire-and-forget Task.Run, giving the host
/// control over in-flight work during shutdown. Each execution gets its own IServiceScope
/// so IExecutionTracker and IDataGateway have correct scoped lifetimes — eliminating the
/// singleton IExecutionTracker hack.
/// </remarks>
public sealed class PipelineExecutionBackgroundService : BackgroundService
{

    private readonly PipelineExecutionQueue _queue;
    private readonly PipelineRunScheduler _scheduler;
    private readonly PipelineRunControl _runControl;
    private readonly IPipelineRunRequestStore _requests;
    private readonly IPipelineRunHolds _holds;
    private readonly PipelineRequestProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PipelineStopGrace _grace;
    private readonly CancellationTokenSource _stopRuns = new();
    private readonly ILogger<PipelineExecutionBackgroundService> _logger;
    private readonly IExecutionCompletionSignaler? _completionSignaler;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionBackgroundService"/>.
    /// </summary>
    public PipelineExecutionBackgroundService(
        PipelineExecutionQueue queue,
        PipelineRunScheduler scheduler,
        PipelineRunControl runControl,
        IPipelineRunRequestStore requests,
        IPipelineRunHolds holds,
        PipelineRequestProcessor processor,
        IServiceScopeFactory scopeFactory,
        PipelineStopGrace grace,
        ILogger<PipelineExecutionBackgroundService> logger,
        IExecutionCompletionSignaler? completionSignaler = null)
    {
        _queue = queue;
        _scheduler = scheduler;
        _runControl = runControl;
        _requests = requests;
        _holds = holds;
        _processor = processor;
        _scopeFactory = scopeFactory;
        _grace = grace;
        _logger = logger ?? NullLogger<PipelineExecutionBackgroundService>.Instance;
        _completionSignaler = completionSignaler;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _stopRuns.Dispose();
        base.Dispose();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EtlLog.BackgroundExecutorStarted(_logger);

        // Why each run on its own task: a run that waits for the pipeline it started has to leave the loop free to start that one.
        // Why a run queues with the scheduler here: runs are queued in the order they were enqueued, and the scheduler starts them as the
        // limits and the tables they write allow (FDW-3085). Only the queueing is in the loop; waiting for a turn is in the run's own task.
        // Why the runs accepted before the host last stopped come first, and not through the queue: they are older than anything the queue holds, and a
        // backlog bigger than the queue's capacity would otherwise wait for a reader that is not started yet (FDW-3216).
        // Why the runs have a token of their own (FDW-3786): the host's stop token ends what the host takes in (the queue, the runs waiting for their turn),
        // but a run in flight is given the stated time to finish before the host stops it, so it follows a second token the host cancels after that time.
        List<Task> running = [];
        try
        {
            foreach (var request in await Recovered(stoppingToken).ConfigureAwait(false))
            {
                running.Add(await StartRun(request, stoppingToken, _stopRuns.Token).ConfigureAwait(false));
            }

            await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                running.RemoveAll(task => task.IsCompleted);
                running.Add(await StartRun(request, stoppingToken, _stopRuns.Token).ConfigureAwait(false));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping: no new run is taken from here on, and the runs in flight are given their time below.
        }

        _ = await PipelineRunDrain.Wait(running, _grace.Duration, () => _stopRuns.Cancel(), _logger).ConfigureAwait(false);
        EtlLog.BackgroundExecutorStopping(_logger);
    }

    // Queues the run with the scheduler, in the order it came, and leaves it waiting for its turn on a task of its own.
    private async Task<Task> StartRun(PipelineExecutionRequest request, CancellationToken stoppingToken, CancellationToken runsToken)
    {
        var admission = await Admit(request, stoppingToken, runsToken).ConfigureAwait(false);
        return Task.Run(() => RunWhenItsTurn(request, admission, stoppingToken, runsToken), CancellationToken.None);
    }

    // The runs the host accepted before it last stopped and did not start, oldest first, each held run with the time it was held until (FDW-3216). A host
    // that cannot read them starts without them and says so: their records stay as they are, and the next start reads them again.
    private async Task<IReadOnlyList<PipelineExecutionRequest>> Recovered(CancellationToken stoppingToken)
    {
        var recovery = await _requests.Recover(stoppingToken).ConfigureAwait(false);
        if (recovery.IsFailure || recovery.Value is null)
        {
            PipelineRunRecoveryLog.RecoveryFailed(_logger, recovery.CurrentMessage);
            return [];
        }

        PipelineRunRecoveryLog.RunsRecovered(
            _logger, recovery.Value.Requests.Count, recovery.Value.Requests.Count(request => request.HeldUntil is not null), recovery.Value.Approvals.Count,
            recovery.Value.WithoutRequest);
        if (recovery.Value.Interrupted.Count > 0)
        {
            PipelineRunRecoveryLog.RunsWereRunning(_logger, recovery.Value.Interrupted.Count);
        }

        foreach (var approval in recovery.Value.Approvals)
        {
            if (_holds.Hold(approval))
            {
                PipelineRunRecoveryLog.ApprovalWaitsAgain(_logger, approval.ExecutionId, approval.PipelineName);
            }
        }

        foreach (var request in recovery.Value.Requests)
        {
            PipelineRunRecoveryLog.RunTakenUp(
                _logger, request.ExecutionId, request.PipelineName, request.HeldUntil is { } until ? "held until " + until.ToString("O", CultureInfo.InvariantCulture) : "waiting for its turn");
        }

        var again = await new PipelineCutOffRuns(_requests, _scopeFactory, _logger).StartAgain(recovery.Value.Interrupted, stoppingToken).ConfigureAwait(false);
        return [.. again, .. recovery.Value.Requests];
    }

    // Reads the limits and works out what the run writes, then queues it with the scheduler. A limit that is not set refuses the run: it
    // is completed as failed naming the setting, and never queued. So is a pipeline that does not state a usable number of parallel runs
    // or overflow behavior, and a run past its pipeline's limit when the pipeline refuses (FDW-3117).
    private async Task<RunAdmission> Admit(PipelineExecutionRequest request, CancellationToken stoppingToken, CancellationToken runsToken)
    {
        // Held from here, while the run waits for its turn, so a cancel reaches a queued run as well as a running one (FDW-2950).
        var run = _runControl.Register(request.ExecutionId, runsToken);
        try
        {
            return await AdmitRegistered(request, run, stoppingToken).ConfigureAwait(false);
        }
        catch
        {
            run.Dispose();
            throw;
        }
    }

    private async Task<RunAdmission> AdmitRegistered(PipelineExecutionRequest request, PipelineRunHandle run, CancellationToken stoppingToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            _processor.EstablishSystemAuthenticationContext(scope.ServiceProvider, request);

            var limits = PipelineRunLimits.Read(scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>(), request.TenantId, _logger);
            if (limits.IsFailure || limits.Value is null)
            {
                return new RunAdmission(null, new PipelineRunRefusal("RunLimitRefused", string.Concat(limits.CurrentMessage)), run);
            }

            var pipeline = await scope.ServiceProvider.GetRequiredService<IPipelineConfigurationProvider>()
                .Get(request.PipelineId, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, stoppingToken).ConfigureAwait(false);
            var claim = await PipelineRunClaims.For(
                request,
                pipeline.IsSuccess ? pipeline.Value : null,
                new LoadTargetResolver(
                    scope.ServiceProvider.GetService<IDataSetConfigurationProvider>(), scope.ServiceProvider.GetService<IDataStoreProvider>()),
                limits.Value,
                _logger,
                stoppingToken).ConfigureAwait(false);
            if (claim.IsFailure || claim.Value is null)
            {
                return new RunAdmission(null, new PipelineRunRefusal("PipelineParallelismInvalid", string.Concat(claim.CurrentMessage)), run);
            }

            var place = _scheduler.Queue(claim.Value, request.HeldUntil);
            if (request.HeldUntil is null && place.HeldUntil is { } until)
            {
                await RecordHold(request, until, stoppingToken).ConfigureAwait(false);
            }

            return place.IsRefused ? Refused(request, place, run) : new RunAdmission(place, null, run);
        }
    }

    // A run the scheduler held back keeps the time it is held until on its record, so a restart puts it back for that time (FDW-3216). A hold that cannot
    // be recorded is logged by the store and here; the run is held all the same, and a restart would take it up as a run waiting for its turn.
    private async Task RecordHold(PipelineExecutionRequest request, DateTimeOffset until, CancellationToken stoppingToken)
    {
        var recorded = await _requests.RecordHeld(request, until, stoppingToken).ConfigureAwait(false);
        if (recorded.IsFailure)
        {
            PipelineRunRecoveryLog.HoldNotRecorded(_logger, request.ExecutionId, until, recorded.CurrentMessage);
        }
    }

    private RunAdmission Refused(PipelineExecutionRequest request, PipelineRunPlace place, PipelineRunHandle run)
    {
        PipelineRunSchedulerLog.RunRefused(_logger, request.PipelineName, place.RefusedLimit);
        return new RunAdmission(
            null,
            new PipelineRunRefusal("RunRefused", $"Pipeline '{request.PipelineName}' already has {place.RefusedLimit} runs active, its limit, and its overflow behavior is Refuse."),
            run);
    }

    private async Task RunWhenItsTurn(PipelineExecutionRequest request, RunAdmission admission, CancellationToken stoppingToken, CancellationToken runsToken)
    {
        using var run = admission.Run;
        if (admission.Place is not { } place)
        {
            await _processor.ProcessRequest(request, admission.Refusal, run, runsToken).ConfigureAwait(false);
            return;
        }

        try
        {
            // Why the host's stop token as well (FDW-3786): a run that has not started does not start while the host drains; it stays as it was, with its request.
            using var waiting = CancellationTokenSource.CreateLinkedTokenSource(run.Token, stoppingToken);
            await _scheduler.Wait(place, waiting.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException ex) when (run.IsCancelled)
        {
            // Cancelled while it waited: it never ran, so the row only needs closing and anyone waiting on it needs telling.
            PipelineRunControlLog.RunCancelledWhileWaiting(_logger, ex, request.ExecutionId, request.PipelineName);
            await CancelWaiting(request, stoppingToken).ConfigureAwait(false);
            return;
        }

        _scheduler.Hold(place);
        try
        {
            await _processor.ProcessRequest(request, null, run, runsToken).ConfigureAwait(false);
        }
        finally
        {
            _scheduler.Release(place);
        }
    }

    private sealed record RunAdmission(PipelineRunPlace? Place, PipelineRunRefusal? Refusal, PipelineRunHandle Run);

    // A run cancelled before it got its place: closes it the way a cancelled run is closed.
    private async Task CancelWaiting(PipelineExecutionRequest request, CancellationToken stoppingToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            _processor.EstablishSystemAuthenticationContext(scope.ServiceProvider, request);
            var orgId = await _processor.ResolveOwningOrg(scope.ServiceProvider, request, stoppingToken).ConfigureAwait(false);
            await PipelineRequestProcessor.HandleCancellation(
                scope.ServiceProvider.GetRequiredService<IExecutionTracker>(),
                scope.ServiceProvider.GetRequiredService<IDataGatewayProvider>(),
                scope.ServiceProvider.GetRequiredService<IPipelineStatusBroadcaster>(),
                request, orgId, PipelineRequestProcessor.CancelledCode, PipelineRequestProcessor.CancelledReason, _completionSignaler, _logger).ConfigureAwait(false);
            _ = await _requests.Forget(request.ExecutionId, stoppingToken).ConfigureAwait(false);
        }
    }

}
