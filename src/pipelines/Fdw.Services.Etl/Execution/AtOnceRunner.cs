using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The runner of the AtOnce run mode (FDW-6141): opens the run as every run is opened, answers the caller with its execution, and runs the pipeline at once, in the host
/// that took the request, through the shared run code. It takes no place from the scheduler and uses no queue.
/// </summary>
/// <remarks>
/// <para>
/// The run is held by <see cref="PipelineRunControl"/> while it runs, so a stop reaches it as it reaches a queued run, and by <see cref="PipelineAtOnceRuns"/>, which gives it the
/// token the host's stop cancels and waits for it. Its time limit is the one its pipeline states (a caller can only shorten it), judged against the ceiling in force now: a
/// pipeline that states none, a limit above a ceiling and a ceiling that is not set each refuse the run before anything is opened, and a run is never started with a lower limit.
/// </para>
/// <para>
/// Why it is in the package of the Interactive type and not a package of its own: a pipeline type and the runner of its run mode are never separated by a package boundary, or a
/// host that references the type would be refused at start for a run mode nothing carries out.
/// </para>
/// </remarks>
internal sealed class AtOnceRunner : IPipelineRunner
{
    private readonly PipelineRunOpening _opening;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IPipelineRequestProcessor _processor;
    private readonly PipelineRunControl _control;
    private readonly PipelineAtOnceRuns _runs;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IExecutionCompletionSignaler? _signaler;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AtOnceRunner"/> class.</summary>
    /// <param name="tracker">Creates and closes the execution item of the run.</param>
    /// <param name="dataGateways">Writes the run row.</param>
    /// <param name="origins">Makes what the run's row states about where it came from.</param>
    /// <param name="broadcaster">Announces that the run was triggered.</param>
    /// <param name="processor">Runs the request through the engine.</param>
    /// <param name="control">Holds the run so a stop reaches it.</param>
    /// <param name="runs">Holds the run for the host and gives it the token the host's stop cancels.</param>
    /// <param name="settings">The settings the ceilings on the limits are read from.</param>
    /// <param name="signaler">Keeps a run's completion for a caller that awaits it (optional).</param>
    /// <param name="logger">The logger.</param>
    public AtOnceRunner(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineRunOrigins origins,
        IPipelineStatusBroadcaster broadcaster,
        IPipelineRequestProcessor processor,
        PipelineRunControl control,
        PipelineAtOnceRuns runs,
        IEffectiveSettingsProvider settings,
        IExecutionCompletionSignaler? signaler = null,
        ILogger<AtOnceRunner>? logger = null)
    {
        _logger = (ILogger?)logger ?? NullLogger.Instance;
        _opening = new PipelineRunOpening(tracker, dataGateways, origins, settings, signaler, RunMode.Name, _logger);
        _broadcaster = broadcaster;
        _processor = processor;
        _control = control;
        _runs = runs;
        _settings = settings;
        _signaler = signaler;
    }

    /// <inheritdoc />
    public IPipelineRunMode RunMode { get; } = PipelineRunModes.ByName("AtOnce");

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Start(PipelineStart start, IPipelineImplementationConfiguration pipeline, CancellationToken cancellationToken = default)
    {
        var limits = JudgeLimits(start, pipeline);
        if (limits.IsFailure || limits.Value is not { } stated)
            return limits.ToNewResult<Guid>();

        var opened = await Open(start, pipeline, cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure || opened.Value is not { } run)
            return opened.ToNewResult<Guid>();

        var request = PipelineRunOpening.RequestOf(start, pipeline, run.ExecutionId, run.Values, stated.TimeoutSeconds, stated.MaxSteps, stated.Limits);
        await _broadcaster.BroadcastRunStatus(pipeline, run.ExecutionId, "Triggered").ConfigureAwait(false);

        var handle = _control.Register(run.ExecutionId, _runs.Stopping);
        var held = _runs.HoldLimits(run.ExecutionId, stated.Limits);
        _runs.Run(() => RunAndRelease(request, handle, held));
        PipelineRunStartLog.AtOnceRunStarted(_logger, pipeline.Name, run.ExecutionId, start.TriggerSource);
        return GenericResult<Guid>.Success(run.ExecutionId);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> Await(Guid executionId, CancellationToken cancellationToken = default)
    {
        if (_signaler is not { } signaler)
            return GenericResult<bool>.Failure(PipelineRunStartLog.RunCompletionNotKept(_logger, executionId));

        try
        {
            // The run holds no place of the scheduler, so there is none to hand back while it waits.
            return GenericResult<bool>.Success(await signaler.Await(executionId, cancellationToken).ConfigureAwait(false));
        }
        finally
        {
            signaler.Deregister(executionId);
        }
    }

    // The limits the pipeline states, judged against the ceilings in force now, before anything is written. A caller that states a shorter time limit shortens it.
    // A pipeline an at-once run starts (FDW-6701) is not judged against the ceilings: it takes its parent's limits, which were, and what its own pipeline states can only shorten them.
    private IGenericResult<StatedLimits> JudgeLimits(PipelineStart start, IPipelineImplementationConfiguration pipeline)
    {
        if (start.ParentLimits is { } parent)
            return LimitsUnder(parent, start, pipeline);

        if (pipeline is not IRunTimeLimitPipelineConfiguration { MaxRunSeconds: { } seconds })
            return GenericResult<StatedLimits>.Failure(PipelineRunLog.AtOnceLimitNotStated(_logger, pipeline.Name, "time limit (MaxRunSeconds)"));

        if (pipeline is not IGraphPipelineConfiguration { MaxSteps: { } steps })
            return GenericResult<StatedLimits>.Failure(PipelineRunLog.AtOnceLimitNotStated(_logger, pipeline.Name, "step limit (MaxSteps)"));

        var ceilings = PipelineRunLimitCeilings.Read(_settings, start.TenantId, _logger);
        if (ceilings.IsFailure || ceilings.Value is not { } inForce)
            return ceilings.ToNewResult<StatedLimits>();

        var judged = PipelineRunLimitCeilings.Judge(pipeline.Name, seconds, steps, inForce, _logger);
        if (judged.IsFailure)
            return judged.ToNewResult<StatedLimits>();

        var timeout = start.TimeoutSeconds is { } asked && asked < seconds ? asked : seconds;
        return GenericResult<StatedLimits>.Success(new StatedLimits(timeout, steps, PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, timeout, steps)));
    }

    // The limits of a pipeline started under an at-once run: its parent's, narrowed by what its own pipeline states and by a shorter time its caller asked for. A parent with no
    // time or no step left starts nothing.
    private IGenericResult<StatedLimits> LimitsUnder(PipelineRunTimeAndStepLimits parent, PipelineStart start, IPipelineImplementationConfiguration pipeline)
    {
        var now = DateTimeOffset.UtcNow;
        int? own = pipeline is IRunTimeLimitPipelineConfiguration { MaxRunSeconds: { } stated } ? stated : null;
        int? seconds = start.TimeoutSeconds is { } asked && (own is null || asked < own) ? asked : own;
        int? steps = pipeline is IGraphPipelineConfiguration { MaxSteps: { } statedSteps } ? statedSteps : null;

        var child = parent.ForChild(now, seconds, steps);
        if (child.Deadline <= now)
            return GenericResult<StatedLimits>.Failure(PipelineRunLog.AtOnceParentLimitsUsedUp(_logger, pipeline.Name, "time"));

        if (child.StepsLeft <= 0)
            return GenericResult<StatedLimits>.Failure(PipelineRunLog.AtOnceParentLimitsUsedUp(_logger, pipeline.Name, "steps"));

        return GenericResult<StatedLimits>.Success(new StatedLimits((int)Math.Ceiling((child.Deadline - now).TotalSeconds), child.StepsLeft, child));
    }

    // The run's record, as every run's is opened; a caller that opened it already (the trigger does) is run as it prepared it, with no second record.
    private async Task<IGenericResult<OpenedRun>> Open(PipelineStart start, IPipelineImplementationConfiguration pipeline, CancellationToken ct)
    {
        if (start.AwaitCompletion && _signaler is null)
            return GenericResult<OpenedRun>.Failure(PipelineRunStartLog.CompletionNotAwaitable(_logger, pipeline.Name));

        if (start.Opened is { } given)
        {
            Keep(start, given.ExecutionId);
            return GenericResult<OpenedRun>.Success(new OpenedRun(given.ExecutionId, given.ResolvedParameters));
        }

        var planned = await _opening.Plan(start, pipeline, ct).ConfigureAwait(false);
        if (planned.IsFailure || planned.Value is not { } plan)
            return planned.ToNewResult<OpenedRun>();

        var executionId = await _opening.Open(start, plan, ct).ConfigureAwait(false);
        if (executionId.IsFailure)
            return executionId.ToNewResult<OpenedRun>();

        Keep(start, executionId.Value);
        await _opening.RecordGivenParameters(start, plan, executionId.Value, ct).ConfigureAwait(false);
        return GenericResult<OpenedRun>.Success(new OpenedRun(executionId.Value, plan.Values));
    }

    // Keeps the run's completion for a caller that awaits it; the caller then awaits, so it is released.
    private void Keep(PipelineStart start, Guid executionId)
    {
        if (start.AwaitCompletion)
            _signaler?.Register(executionId);
    }

    private async Task RunAndRelease(PipelineExecutionRequest request, PipelineRunHandle handle, IDisposable heldLimits)
    {
        using (heldLimits)
        using (handle)
        {
            await _processor.ProcessRequest(request, null, handle, _runs.Stopping).ConfigureAwait(false);
        }
    }

    private sealed record StatedLimits(int TimeoutSeconds, int MaxSteps, PipelineRunTimeAndStepLimits Limits);

    private sealed record OpenedRun(Guid ExecutionId, IReadOnlyDictionary<string, object?>? Values);
}
