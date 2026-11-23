using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Pipelines;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The runner of the Queued run mode (FDW-6131): records the run, keeps and queues its request, and leaves it to the host's executor
/// (<see cref="PipelineExecutionBackgroundService"/>), which runs it when its turn comes.
/// </summary>
/// <remarks>
/// <para>
/// A run whose execution the runner opens is: an execution item (the child of the run that started it and carrying its correlation, when one did), the run
/// row that holds its start and end, the parameters it was given on its record, and a queued request that carries the tenant, the roles and the typed
/// parameters. A run whose execution its caller opened (<see cref="PipelineStart.Opened"/>) is queued as the caller prepared it: no second record, no
/// value read again, no parameter recorded again.
/// </para>
/// <para>
/// Why the runner refuses instead of choosing: the tenant, the roles, the cause and the pipeline are the caller's to state. A start that leaves one out is
/// not given the surroundings' (no ambient tenant, no empty role list).
/// </para>
/// </remarks>
public sealed class QueuedPipelineRunner : IPipelineRunner
{
    private readonly IPipelineExecutionQueue _queue;
    private readonly PipelineRunScheduler _scheduler;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IExecutionCompletionSignaler? _signaler;
    private readonly PipelineRunOpening _opening;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="QueuedPipelineRunner"/> class.</summary>
    /// <param name="tracker">Creates and closes the execution item of the run.</param>
    /// <param name="dataGateways">Writes the run row.</param>
    /// <param name="queue">Takes the request the executor runs.</param>
    /// <param name="scheduler">Decides how many pipelines run at once; a run that waits on a pipeline it started hands its place back.</param>
    /// <param name="broadcaster">Announces that the run was triggered.</param>
    /// <param name="origins">Makes what the run's row states about where it came from: the person, the run mode, the host and the slot (FDW-6519).</param>
    /// <param name="settings">The settings the length of a chain of runs is judged against (FDW-6135).</param>
    /// <param name="signaler">Keeps a run's completion for a caller that awaits it (optional; a host that orchestrates has one).</param>
    /// <param name="logger">The logger.</param>
    public QueuedPipelineRunner(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineExecutionQueue queue,
        PipelineRunScheduler scheduler,
        IPipelineStatusBroadcaster broadcaster,
        IPipelineRunOrigins origins,
        IEffectiveSettingsProvider settings,
        IExecutionCompletionSignaler? signaler = null,
        ILogger<QueuedPipelineRunner>? logger = null)
        : this(tracker, dataGateways, queue, scheduler, broadcaster, origins, settings, signaler, (ILogger?)logger)
    {
    }

    // The run starter adapter builds its runner with its own logger, so what it logs keeps the category it always had.
    internal QueuedPipelineRunner(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineExecutionQueue queue,
        PipelineRunScheduler scheduler,
        IPipelineStatusBroadcaster broadcaster,
        IPipelineRunOrigins origins,
        IEffectiveSettingsProvider settings,
        IExecutionCompletionSignaler? signaler,
        ILogger? logger)
    {
        _queue = queue;
        _scheduler = scheduler;
        _broadcaster = broadcaster;
        _opening = new PipelineRunOpening(tracker, dataGateways, origins, settings, signaler, RunMode.Name, logger ?? NullLogger.Instance);
        _signaler = signaler;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public IPipelineRunMode RunMode { get; } = PipelineRunModes.ByName("Queued");

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Start(PipelineStart start, IPipelineImplementationConfiguration pipeline, CancellationToken cancellationToken = default)
    {
        if (start.Opened is { } opened)
            return await QueueOpened(start, pipeline, opened, cancellationToken).ConfigureAwait(false);

        var planned = await _opening.Plan(start, pipeline, cancellationToken).ConfigureAwait(false);
        if (planned.IsFailure || planned.Value is not { } plan)
            return planned.ToNewResult<Guid>();

        var executionId = await _opening.Open(start, plan, cancellationToken).ConfigureAwait(false);
        return executionId.IsFailure
            ? executionId
            : await Queue(start, plan, executionId.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> Await(Guid executionId, CancellationToken cancellationToken = default)
    {
        if (_signaler is not { } signaler)
            return GenericResult<bool>.Failure(PipelineRunStartLog.RunCompletionNotKept(_logger, executionId));

        try
        {
            // The run that waits hands its place back, so the pipeline it started can run; it takes the place again when that ends.
            var handedBack = _scheduler.Yield();
            await using (handedBack.ConfigureAwait(false))
            {
                return GenericResult<bool>.Success(await signaler.Await(executionId, cancellationToken).ConfigureAwait(false));
            }
        }
        finally
        {
            signaler.Deregister(executionId);
        }
    }

    // The run is announced and handed to the executor; a queue that is full closes the item it opened.
    private async Task<IGenericResult<Guid>> Queue(PipelineStart start, PlannedPipelineRun planned, Guid executionId, CancellationToken ct)
    {
        var pipeline = planned.Pipeline;
        var signaler = start.AwaitCompletion ? _signaler : null;
        signaler?.Register(executionId);

        await _broadcaster.BroadcastRunStatus(pipeline, executionId, "Triggered").ConfigureAwait(false);
        await _opening.RecordGivenParameters(start, planned, executionId, ct).ConfigureAwait(false);

        var enqueued = await _queue.Enqueue(PipelineRunOpening.RequestOf(start, pipeline, executionId, planned.Values, start.TimeoutSeconds, maxSteps: null), ct).ConfigureAwait(false);
        if (!enqueued.IsAccepted)
        {
            signaler?.Deregister(executionId);
            return await _opening.Refuse(
                executionId, enqueued.Code,
                GenericResult.Failure(PipelineRunStartLog.RunNotTaken(_logger, pipeline.Name, executionId, enqueued.Reason))).ConfigureAwait(false);
        }

        if (start.ParentExecutionId is { } parent)
            PipelineRunStartLog.RunStarted(_logger, pipeline.Name, executionId, parent, start.TriggerSource);
        else
            PipelineRunStartLog.RunQueued(_logger, pipeline.Name, executionId, start.TriggerSource);

        return GenericResult<Guid>.Success(executionId);
    }

    // A run whose caller opened its execution, judged it and recorded what it was given: only the queue is left. A refusal is the caller's to answer
    // (a trigger tells a full queue from stopped runs, an approval keeps the request for the next decision), so the item is left as it is.
    private async Task<IGenericResult<Guid>> QueueOpened(PipelineStart start, IPipelineImplementationConfiguration pipeline, PipelineStartOpened opened, CancellationToken ct)
    {
        var executionId = opened.ExecutionId;
        if (start.AwaitCompletion && _signaler is null)
            return GenericResult<Guid>.Failure(PipelineRunStartLog.CompletionNotAwaitable(_logger, pipeline.Name));

        var signaler = start.AwaitCompletion ? _signaler : null;
        signaler?.Register(executionId);

        var enqueued = await _queue.Enqueue(PipelineRunOpening.RequestOf(start, pipeline, executionId, opened.ResolvedParameters, start.TimeoutSeconds, maxSteps: null), ct).ConfigureAwait(false);
        if (enqueued.IsAccepted)
        {
            PipelineRunStartLog.OpenedRunQueued(_logger, pipeline.Name, executionId, start.TriggerSource);
            return GenericResult<Guid>.Success(executionId);
        }

        signaler?.Deregister(executionId);
        PipelineRunStartLog.OpenedRunNotQueued(_logger, pipeline.Name, executionId, enqueued.Code);
        return GenericResult<Guid>.Failure(OpenedRefusal(enqueued));
    }

    /// <summary>
    /// Reads back the queue's refusal of a run whose caller opened its execution (FDW-6131), so the caller answers it as it answered the queue: a full
    /// queue, stopped runs, a refusal the queue could not record.
    /// </summary>
    /// <param name="started">What <see cref="Start"/> answered.</param>
    /// <param name="refusal">The queue's refusal, with its code and reason; none when the start was not refused by the queue.</param>
    /// <returns><see langword="true"/> when the start was refused by the queue.</returns>
    public static bool IsQueueRefusal(IGenericResult started, [NotNullWhen(true)] out PipelineEnqueueResult? refusal)
    {
        refusal = started.FirstFailureMessage() is { } failure ? RefusalOf(failure) : null;
        return refusal is not null;
    }

    // Each reason the queue gives is its own coded refusal, so a caller tells them apart by the number and never by the text; the text is the queue's reason.
    private IGenericMessage OpenedRefusal(PipelineEnqueueResult enqueued) =>
        string.Equals(enqueued.Code, PipelineEnqueueResult.QueueFullCode, StringComparison.Ordinal)
            ? PipelineRunStartLog.OpenedRunQueueFull(_logger, enqueued.Reason)
            : string.Equals(enqueued.Code, PipelineEnqueueResult.ServerBusyCode, StringComparison.Ordinal)
                ? PipelineRunStartLog.OpenedRunStopped(_logger, enqueued.Reason)
                : PipelineRunStartLog.OpenedRunRefusalNotRecorded(_logger, enqueued.Reason);

    // The numbers are the EventIds of the three refusals above (PipelineRunStartLog); QueuedPipelineRunnerTests reads each back through a real start.
    // A number no QueueRefusal holds is not a refusal of the queue.
    private static PipelineEnqueueResult? RefusalOf(FailureMessage failure) =>
        ReferenceEquals(QueueRefusals.ByNumber(failure.Number), QueueRefusals.NotFound) ? null : QueueRefusals.ByNumber(failure.Number).Refusal(failure.Message.Message);

}
