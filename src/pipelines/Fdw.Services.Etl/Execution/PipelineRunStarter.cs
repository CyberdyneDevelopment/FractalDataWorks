using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
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
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Starts a pipeline run under another execution (FDW-1381): the shared start of an orchestration node's member pipelines and of
/// a StartPipeline stage's target.
/// </summary>
/// <remarks>
/// <para>
/// A started run is: an execution item that is the child of the run that started it and carries its correlation, the run row
/// that holds its start and end, and a queued request that carries the tenant, the roles and the typed parameters. The
/// executor stamps the run row's start when the run reaches Running and its end when it ends, so a started run has the same
/// start and end a triggered one has.
/// </para>
/// <para>
/// Why the start refuses instead of choosing: the tenant, the roles, the cause and the pipeline are the caller's to state. A
/// start that leaves one out is not given the surroundings' (no ambient tenant, no empty role list).
/// </para>
/// </remarks>
public sealed class PipelineRunStarter : IPipelineRunStarter
{
    private const string RunContainerName = "PipelineExecution";
    private const string RunPath = "etl";

    private readonly IExecutionTracker _tracker;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly IPipelineExecutionQueue _queue;
    private readonly PipelineRunScheduler _scheduler;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IExecutionCompletionSignaler? _signaler;
    private readonly ILogger<PipelineRunStarter> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunStarter"/> class.</summary>
    /// <param name="tracker">Creates the execution item of the run.</param>
    /// <param name="dataGateways">Writes the run row.</param>
    /// <param name="pipelines">Finds the pipeline to start and the parameters it declares.</param>
    /// <param name="queue">Takes the request the executor runs.</param>
    /// <param name="scheduler">Decides how many pipelines run at once; a run that waits on a pipeline it started hands its place back.</param>
    /// <param name="broadcaster">Announces that the run was triggered.</param>
    /// <param name="signaler">Keeps a run's completion for a caller that awaits it (optional; a host that orchestrates has one).</param>
    /// <param name="logger">The logger.</param>
    public PipelineRunStarter(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineConfigurationProvider pipelines,
        IPipelineExecutionQueue queue,
        PipelineRunScheduler scheduler,
        IPipelineStatusBroadcaster broadcaster,
        IExecutionCompletionSignaler? signaler = null,
        ILogger<PipelineRunStarter>? logger = null)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _signaler = signaler;
        _logger = logger ?? NullLogger<PipelineRunStarter>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Start(PipelineRunStart start, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(start);

        var planned = await Plan(start, cancellationToken).ConfigureAwait(false);
        if (planned.IsFailure)
            return planned.ToNewResult<Guid>();

        var opened = await Open(start, planned.Value!, cancellationToken).ConfigureAwait(false);
        return opened.IsFailure
            ? opened
            : await Queue(start, planned.Value!, opened.Value, cancellationToken).ConfigureAwait(false);
    }

    // What the start needs before anything is written: the pipeline, its typed parameters, a host that can await and keep the row.
    private async Task<IGenericResult<PlannedRun>> Plan(PipelineRunStart start, CancellationToken ct)
    {
        var named = await Find(start, ct).ConfigureAwait(false);
        if (named.IsFailure)
            return named.ToNewResult<PlannedRun>();

        var pipeline = named.Value!;
        var cycle = await CheckAncestors(start.ParentExecutionId, pipeline.Name, pipeline.Id, ct).ConfigureAwait(false);
        if (cycle.IsFailure)
            return cycle.ToNewResult<PlannedRun>();

        // Why a refusal: a pipeline that requires approval waits for a person, and a run started from inside another run (or by project
        // orchestration) has none to wait for. Starting it anyway would run it without the approval its author asked for (FDW-1937).
        if (pipeline is IApprovalGatedPipelineConfiguration { RequireApprovalToRun: true })
            return GenericResult<PlannedRun>.Failure(PipelineRunStartLog.PipelineRequiresApproval(_logger, pipeline.Name));

        var declared = (pipeline as IDeclaresRunParameters)?.DeclaredRunParameters;
        var values = PipelineRunParameterValues.Resolve(declared, start.Parameters, _logger);
        if (values.IsFailure)
            return values.ToNewResult<PlannedRun>();

        if (start.AwaitCompletion && _signaler is null)
            return GenericResult<PlannedRun>.Failure(PipelineRunStartLog.CompletionNotAwaitable(_logger, pipeline.Name));

        var store = EtlPipelineTypes.OperationalConnection;
        return string.IsNullOrWhiteSpace(store)
            ? GenericResult<PlannedRun>.Failure(PipelineRunStartLog.OperationalStoreNotNamed(_logger, pipeline.Name))
            : GenericResult<PlannedRun>.Success(new PlannedRun(pipeline, values.Value!, store, declared));
    }

    // A pipeline that starts itself, or one of the pipelines that started it, would never end: each run would start the next.
    // The chain of execution items above the starter is read and a run of the same pipeline among them is a refusal.
    // A run's item names its pipeline by the name or the id it was triggered by, so both are compared.
    private async Task<IGenericResult> CheckAncestors(Guid parentExecutionId, string pipelineName, Guid pipelineId, CancellationToken ct)
    {
        var idText = pipelineId.ToString("D", CultureInfo.InvariantCulture);
        Guid? current = parentExecutionId;
        while (current is { } id)
        {
            var read = await _tracker.GetItem(id, ct).ConfigureAwait(false);
            if (read.IsFailure || read.Value is null)
            {
                return GenericResult.Failure(PipelineRunStartLog.PipelineNotStartable(
                    _logger, pipelineName, $"the execution it would be started under could not be read: {read.CurrentMessage}"));
            }

            var ancestor = read.Value;
            if (ancestor.Parameters.TryGetValue("PipelineName", out var ran)
                && (string.Equals(ran as string, pipelineName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ran as string, idText, StringComparison.OrdinalIgnoreCase)))
            {
                return GenericResult.Failure(PipelineRunStartLog.PipelineIsAncestor(_logger, pipelineName, id));
            }

            current = ancestor.ParentId;
        }

        return GenericResult.Success();
    }

    // The execution item (a child of the starter's, in its tenant, carrying its correlation) and the run row beside it.
    private async Task<IGenericResult<Guid>> Open(PipelineRunStart start, PlannedRun planned, CancellationToken ct)
    {
        var name = planned.Pipeline.Name;
        var created = await _tracker.CreateItem(
            ExecutionItemTypes.Job,
            $"{name}_{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}",
            start.TenantId,
            parentId: start.ParentExecutionId,
            correlationId: start.ParentExecutionId.ToString("N"),
            triggerSource: start.TriggerSource,
            parameters: new Dictionary<string, object?>(StringComparer.Ordinal) { ["PipelineName"] = name },
            cancellationToken: ct).ConfigureAwait(false);
        if (created.IsFailure || created.Value is null)
            return GenericResult<Guid>.Failure(PipelineRunStartLog.ExecutionNotCreated(
                _logger, name, start.ParentExecutionId, created.CurrentMessage));

        var executionId = created.Value.Id;
        var written = await WriteRunRow(planned.Store, executionId, name, created.Value.State.Name, ct).ConfigureAwait(false);
        return written.IsFailure
            ? await Refuse(executionId, "RunRowNotWritten", written).ConfigureAwait(false)
            : GenericResult<Guid>.Success(executionId);
    }

    // The run is announced and handed to the executor; a queue that is full closes the item it opened.
    private async Task<IGenericResult<Guid>> Queue(PipelineRunStart start, PlannedRun planned, Guid executionId, CancellationToken ct)
    {
        var pipeline = planned.Pipeline;
        if (start.AwaitCompletion)
            _signaler!.Register(executionId);

        await _broadcaster.BroadcastStatusChange(pipeline.Name, executionId, "Triggered").ConfigureAwait(false);
        await RecordGivenParameters(start, planned, executionId, ct).ConfigureAwait(false);

        var enqueued = await _queue.Enqueue(
            new PipelineExecutionRequest
            {
                ExecutionId = executionId,
                PipelineId = pipeline.Id,
                PipelineName = pipeline.Name,
                TriggerSource = start.TriggerSource,
                RunRoles = start.RunRoles,
                TenantId = start.TenantId,
                Parameters = planned.Values
            }, ct).ConfigureAwait(false);
        if (!enqueued.IsAccepted)
        {
            if (start.AwaitCompletion)
                _signaler!.Deregister(executionId);
            return await Refuse(
                executionId, enqueued.Code,
                GenericResult.Failure(PipelineRunStartLog.RunNotTaken(_logger, pipeline.Name, executionId, enqueued.Reason))).ConfigureAwait(false);
        }

        PipelineRunStartLog.RunStarted(_logger, pipeline.Name, executionId, start.ParentExecutionId, start.TriggerSource);
        return GenericResult<Guid>.Success(executionId);
    }

    // What the started run was given, on its record, as a triggered run's is (FDW-1756, FDW-1992): each declared parameter it was given, and a parameter that
    // took its declared default recorded as not given. Its failure is logged, not fatal: the run is already accepted and the entries only describe it.
    private async Task RecordGivenParameters(PipelineRunStart start, PlannedRun planned, Guid executionId, CancellationToken ct)
    {
        var defaulted = PipelineRunParameterValues.Defaulted(planned.Declared, start.Parameters.Select(supplied => supplied.Key));
        if (RunParameterEntries.EventData(planned.Declared, planned.Values, defaulted) is not { } data)
            return;

        var recorded = await _tracker.RecordEvent(
            executionId, RunParameterEntries.EventType, "Parameters the run was given", data, cancellationToken: ct).ConfigureAwait(false);
        if (recorded.IsFailure)
            PipelineRunStartLog.RunParametersNotRecorded(_logger, planned.Pipeline.Name, executionId, recorded.CurrentMessage);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Check(string pipelineName, IReadOnlyList<string> parameterNames, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(parameterNames);

        var found = await _pipelines.Get(pipelineName, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
            return GenericResult.Failure(PipelineRunStartLog.PipelineNotStartable(_logger, pipelineName, NotFoundReason(found)));

        var declared = (found.Value as IDeclaresRunParameters)?.DeclaredRunParameters;
        return PipelineRunParameterValues.VerifyNames(declared, parameterNames, _logger);
    }

    /// <inheritdoc />
    public async Task<bool> Await(Guid executionId, CancellationToken cancellationToken = default)
    {
        if (_signaler is null)
            return false;

        try
        {
            // The run that waits hands its place back, so the pipeline it started can run; it takes the place again when that ends.
            var handedBack = _scheduler.Yield();
            await using (handedBack.ConfigureAwait(false))
            {
                return await _signaler.Await(executionId, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _signaler.Deregister(executionId);
        }
    }

    // By id or by name, never both and never neither: which of them the caller named is part of what it asked for.
    private async Task<IGenericResult<IPipelineImplementationConfiguration>> Find(PipelineRunStart start, CancellationToken ct)
    {
        var byId = start.PipelineId is not null;
        var byName = !string.IsNullOrWhiteSpace(start.PipelineName);
        if (byId == byName)
        {
            return GenericResult<IPipelineImplementationConfiguration>.Failure(PipelineRunStartLog.PipelineNotNamed(
                _logger, start.ParentExecutionId, byId ? "both" : "neither"));
        }

        var found = start.PipelineId is { } id
            ? await _pipelines.Get(id, ct).ConfigureAwait(false)
            : await _pipelines.Get(start.PipelineName!, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            var label = start.PipelineName ?? start.PipelineId!.Value.ToString("D", CultureInfo.InvariantCulture);
            return GenericResult<IPipelineImplementationConfiguration>.Failure(
                PipelineRunStartLog.PipelineNotStartable(_logger, label, NotFoundReason(found)));
        }

        return GenericResult<IPipelineImplementationConfiguration>.Success(found.Value);
    }

    // The provider answers a pipeline it cannot find with no message; the refusal says what that means.
    private static string NotFoundReason(IGenericResult found) =>
        found.CurrentMessage is { Length: > 0 } message ? message : "no pipeline of that name is visible to the run";

    private async Task<IGenericResult> WriteRunRow(string store, Guid executionId, string pipelineName, string state, CancellationToken ct)
    {
        var row = new EtlPipelineExecutionRecord { Id = executionId, PipelineName = pipelineName, Status = state };
        var insert = Insert.Into<EtlPipelineExecutionRecord>(RunContainerName)
            .DataStore(store)
            .Path(RunPath)
            .Value(row);
        var written = await _dataGateways.ByName("Main").Execute<int>(insert, ct).ConfigureAwait(false);
        return written.IsFailure
            ? GenericResult.Failure(PipelineRunStartLog.RunRowNotWritten(_logger, pipelineName, executionId, written.CurrentMessage))
            : GenericResult.Success();
    }

    // The execution item exists by now, so a start that is refused afterwards closes it as failed with the reason.
    private async Task<IGenericResult<Guid>> Refuse(Guid executionId, string code, IGenericResult refusal)
    {
        var closed = await _tracker.Complete(executionId, false, code, refusal.CurrentMessage, CancellationToken.None).ConfigureAwait(false);
        if (closed.IsFailure)
            EtlLog.CompletionRecordFailed(_logger, executionId, closed.CurrentMessage);

        return refusal.ToNewResult<Guid>();
    }

    private sealed record PlannedRun(
        IPipelineImplementationConfiguration Pipeline, IReadOnlyDictionary<string, object?> Values, string Store, IReadOnlyList<IPipelineRunParameter>? Declared);
}
