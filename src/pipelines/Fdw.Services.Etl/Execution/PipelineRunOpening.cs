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
/// Opens a pipeline run for a run mode's runner (FDW-6141): judges the start (no cycle, no approval to wait for, its typed parameters, the operational store, the
/// origin its row will state), opens its execution item and run row, records the parameters it was given, and closes the item as failed when a start is refused
/// after it was opened.
/// </summary>
/// <remarks>
/// The code both the Queued and the AtOnce runner use, so a run that waits for its turn and a run that starts at once are opened the same way; what differs is what
/// each does with the run afterwards. Moved out of <see cref="QueuedPipelineRunner"/> unchanged.
/// </remarks>
internal sealed class PipelineRunOpening
{
    private const string RunContainerName = "PipelineExecution";
    private const string RunPath = "etl";

    private readonly IExecutionTracker _tracker;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IPipelineRunOrigins _origins;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IExecutionCompletionSignaler? _signaler;
    private readonly string _runMode;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunOpening"/> class.</summary>
    /// <param name="tracker">Creates and closes the execution item of the run.</param>
    /// <param name="dataGateways">Writes the run row.</param>
    /// <param name="origins">Makes what the run's row states about where it came from.</param>
    /// <param name="settings">The settings the length of a chain of runs is judged against (FDW-6135).</param>
    /// <param name="signaler">Keeps a run's completion for a caller that awaits it, or null when the host keeps none.</param>
    /// <param name="runMode">The name of the run mode of the runner that opens the runs.</param>
    /// <param name="logger">The logger.</param>
    internal PipelineRunOpening(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineRunOrigins origins,
        IEffectiveSettingsProvider settings,
        IExecutionCompletionSignaler? signaler,
        string runMode,
        ILogger logger)
    {
        _tracker = tracker;
        _dataGateways = dataGateways;
        _origins = origins;
        _settings = settings;
        _signaler = signaler;
        _runMode = runMode;
        _logger = logger;
    }

    // What the start needs before anything is written: a pipeline that is not among the runs above it and asks no approval, its typed parameters, a
    // host that can await and keep the row.
    internal async Task<IGenericResult<PlannedPipelineRun>> Plan(PipelineStart start, IPipelineImplementationConfiguration pipeline, CancellationToken ct)
    {
        if (start.ParentExecutionId is { } parent)
        {
            var ancestors = await CheckAncestors(parent, pipeline.Name, pipeline.Id, ct).ConfigureAwait(false);
            if (ancestors.IsFailure)
                return ancestors.ToNewResult<PlannedPipelineRun>();

            // Why the length of the chain is judged here (FDW-6135): every run started under another one is opened by this code, the queued runner's and the at-once runner's alike, and the walk
            // above already counts the pipeline runs the start sits under. The limit is a setting and none is assumed.
            var chain = JudgeChain(start, pipeline.Name, ancestors.Value);
            if (chain.IsFailure)
                return chain.ToNewResult<PlannedPipelineRun>();
        }

        // Why a refusal: a pipeline that requires approval waits for a person, and a run started from inside another run (or by project orchestration)
        // has none to wait for. Starting it anyway would run it without the approval its author asked for (FDW-1937). A person's trigger holds the run
        // for approval itself and starts it, once approved, with the execution it opened.
        if (pipeline is IApprovalGatedPipelineConfiguration { RequireApprovalToRun: true })
            return GenericResult<PlannedPipelineRun>.Failure(PipelineRunStartLog.PipelineRequiresApproval(_logger, pipeline.Name));

        var declared = (pipeline as IDeclaresRunParameters)?.DeclaredRunParameters;
        var values = PipelineRunParameterValues.Resolve(declared, start.Parameters, _logger);
        if (values.IsFailure || values.Value is not { } resolved)
            return values.ToNewResult<PlannedPipelineRun>();

        if (start.AwaitCompletion && _signaler is null)
            return GenericResult<PlannedPipelineRun>.Failure(PipelineRunStartLog.CompletionNotAwaitable(_logger, pipeline.Name));

        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
            return GenericResult<PlannedPipelineRun>.Failure(PipelineRunStartLog.OperationalStoreNotNamed(_logger, pipeline.Name));

        // What the run's row will say about where the run came from, made before anything is written: a run whose row could not state it is refused (FDW-6519).
        var origin = _origins.Of(start.PersonId, _runMode);
        return origin.IsFailure || origin.Value is not { } stated
            ? origin.ToNewResult<PlannedPipelineRun>()
            : GenericResult<PlannedPipelineRun>.Success(new PlannedPipelineRun(pipeline, resolved, store, declared, stated));
    }

    // A pipeline that starts itself, or one of the pipelines that started it, would never end: each run would start the next.
    // The chain of execution items above the starter is read and a run of the same pipeline among them is a refusal.
    // A run's item names its pipeline by the name or the id it was triggered by, so both are compared.
    // The walk also counts the pipeline runs above the start (FDW-6135): an item that names a pipeline is a pipeline run, any other item above it (a project's node, say) is not part of the chain.
    private async Task<IGenericResult<int>> CheckAncestors(Guid parentExecutionId, string pipelineName, Guid pipelineId, CancellationToken ct)
    {
        var idText = pipelineId.ToString("D", CultureInfo.InvariantCulture);
        var runs = 0;
        Guid? current = parentExecutionId;
        while (current is { } id)
        {
            var read = await _tracker.GetItem(id, ct).ConfigureAwait(false);
            if (read.IsFailure || read.Value is not { } ancestor)
            {
                return GenericResult<int>.Failure(PipelineRunStartLog.PipelineNotStartable(
                    _logger, pipelineName, $"the execution it would be started under could not be read: {read.CurrentMessage}"));
            }

            if (ancestor.Parameters.TryGetValue("PipelineName", out var ran))
            {
                runs++;
                if (string.Equals(ran as string, pipelineName, StringComparison.OrdinalIgnoreCase) || string.Equals(ran as string, idText, StringComparison.OrdinalIgnoreCase))
                {
                    return GenericResult<int>.Failure(PipelineRunStartLog.PipelineIsAncestor(_logger, pipelineName, id));
                }
            }

            current = ancestor.ParentId;
        }

        return GenericResult<int>.Success(runs);
    }

    // The run the start would open is the runs above it and itself: the most is the setting's, and a setting that is not set to a positive number refuses the start.
    private IGenericResult JudgeChain(PipelineStart start, string pipelineName, int runsAbove)
    {
        var maximum = _settings.GetEffectiveValue<int>(SettingDefinitions.MaxPipelineChainDepth, start.TenantId);
        if (maximum <= 0)
            return GenericResult.Failure(PipelineRunStartLog.PipelineChainLimitNotSet(_logger, pipelineName, SettingDefinitions.MaxPipelineChainDepth));

        return runsAbove + 1 > maximum
            ? GenericResult.Failure(PipelineRunStartLog.PipelineChainLimitReached(_logger, pipelineName, runsAbove + 1, maximum))
            : GenericResult.Success();
    }

    // The execution item (in the run's tenant; a child of the starter's, carrying its correlation, when a run started it) and the run row beside it.
    internal async Task<IGenericResult<Guid>> Open(PipelineStart start, PlannedPipelineRun planned, CancellationToken ct)
    {
        var name = planned.Pipeline.Name;
        var created = await _tracker.CreateItem(
            ExecutionItemTypes.Job,
            $"{name}_{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}",
            start.TenantId,
            parentId: start.ParentExecutionId,
            correlationId: start.ParentExecutionId?.ToString("N"),
            triggerSource: start.TriggerSource,
            parameters: new Dictionary<string, object?>(StringComparer.Ordinal) { ["PipelineName"] = name },
            cancellationToken: ct).ConfigureAwait(false);
        if (created.IsFailure || created.Value is not { } item)
        {
            return GenericResult<Guid>.Failure(start.ParentExecutionId is { } parent
                ? PipelineRunStartLog.ExecutionNotCreated(_logger, name, parent, created.CurrentMessage)
                : PipelineRunStartLog.RootExecutionNotCreated(_logger, name, created.CurrentMessage));
        }

        var written = await WriteRunRow(planned.Store, item.Id, name, start.TenantId, item.State.Name, planned.Origin, ct).ConfigureAwait(false);
        return written.IsFailure
            ? await Refuse(item.Id, "RunRowNotWritten", written).ConfigureAwait(false)
            : GenericResult<Guid>.Success(item.Id);
    }


    // What the started run was given, on its record, as a triggered run's is (FDW-1756, FDW-1992): each declared parameter it was given, and a parameter that
    // took its declared default recorded as not given. Its failure is logged, not fatal: the run is already accepted and the entries only describe it.
    internal async Task RecordGivenParameters(PipelineStart start, PlannedPipelineRun planned, Guid executionId, CancellationToken ct)
    {
        var defaulted = PipelineRunParameterValues.Defaulted(planned.Declared, start.Parameters.Select(supplied => supplied.Key));
        if (RunParameterEntries.EventData(planned.Declared, planned.Values, defaulted) is not { } data)
            return;

        var recorded = await _tracker.RecordEvent(
            executionId, RunParameterEntries.EventType, "Parameters the run was given", data, cancellationToken: ct).ConfigureAwait(false);
        if (recorded.IsFailure)
            PipelineRunStartLog.RunParametersNotRecorded(_logger, planned.Pipeline.Name, executionId, recorded.CurrentMessage);
    }

    private async Task<IGenericResult> WriteRunRow(string store, Guid executionId, string pipelineName, Guid tenantId, string state, PipelineRunOrigin origin, CancellationToken ct)
    {
        var insert = Insert.Into<EtlPipelineExecutionRecord>(RunContainerName)
            .DataStore(store)
            .Path(RunPath)
            .Value(new EtlPipelineExecutionRecord
            {
                Id = executionId, PipelineName = pipelineName, TenantId = tenantId, Status = state,
                ActingPersonId = origin.ActingPersonId, RunMode = origin.RunMode, ExecutingHost = origin.ExecutingHost, ExecutingSlot = origin.ExecutingSlot,
            });
        var written = await _dataGateways.ByName(DataGatewayNames.Main).Execute<int>(insert, ct).ConfigureAwait(false);
        return written.IsFailure
            ? GenericResult.Failure(PipelineRunStartLog.RunRowNotWritten(_logger, pipelineName, executionId, written.CurrentMessage))
            : GenericResult.Success();
    }

    // The execution item exists by now, so a start that is refused afterwards closes it as failed with the reason.
    internal async Task<IGenericResult<Guid>> Refuse(Guid executionId, string code, IGenericResult refusal)
    {
        var closed = await _tracker.Complete(executionId, false, code, refusal.CurrentMessage, CancellationToken.None).ConfigureAwait(false);
        if (closed.IsFailure)
            EtlLog.CompletionRecordFailed(_logger, executionId, closed.CurrentMessage);

        return refusal.ToNewResult<Guid>();
    }

    /// <summary>The request the run's executor runs: what the caller stated about the run, with the time limit the runner settles on.</summary>
    /// <param name="start">What the caller stated.</param>
    /// <param name="pipeline">The pipeline.</param>
    /// <param name="executionId">The run's execution.</param>
    /// <param name="values">The resolved parameter values.</param>
    /// <param name="timeoutSeconds">The time limit of the run, or null for none.</param>
    /// <param name="maxSteps">The step limit of the run, or null for none.</param>
    /// <param name="limits">The time and step limits object the run takes its steps from (FDW-6701), or null when the run builds them from the two values above.</param>
    /// <returns>The request.</returns>
    internal static PipelineExecutionRequest RequestOf(
        PipelineStart start, IPipelineImplementationConfiguration pipeline, Guid executionId, IReadOnlyDictionary<string, object?>? values, int? timeoutSeconds, int? maxSteps,
        PipelineRunTimeAndStepLimits? limits = null) => new()
    {
        ExecutionId = executionId,
        PipelineId = pipeline.Id,
        PipelineName = pipeline.Name,
        TriggerSource = start.TriggerSource,
        RunRoles = start.RunRoles,
        TenantId = start.TenantId,
        Parameters = values,
        TestRowsPerSource = start.TestRowsPerSource,
        ScheduleName = start.ScheduleName,
        ScheduleId = start.ScheduleId,
        TimeoutSeconds = timeoutSeconds,
        MaxSteps = maxSteps,
        Limits = limits,
        PersonId = start.PersonId
    };
}
