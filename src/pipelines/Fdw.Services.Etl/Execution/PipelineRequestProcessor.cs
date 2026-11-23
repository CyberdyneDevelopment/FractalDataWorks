using System;
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
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution.AtOnceRecovery;
using Fdw.Services.Etl.Execution.Claims;
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
using Fdw.Services.Pipelines.Abstractions.Runs;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Runs one stored pipeline request through the engine: opens its scope, moves its record to Running, executes the pipeline as its tenant run and closes the record
/// on every way a run can end (FDW-6631).
/// </summary>
/// <remarks>
/// The queued executor (<see cref="PipelineExecutionBackgroundService"/>) calls it for a run that has taken its place, and the AtOnce runner calls it for a run that
/// starts at once, so both go through the same code. It holds nothing of the queue: admission, the place and its release stay with the executor.
/// </remarks>
public sealed class PipelineRequestProcessor : IPipelineRequestProcessor
{
    private const string EtlContainerName = "PipelineExecution";
    private const string EtlSchemaPath = "etl";
    internal const string CancelledCode = "Cancelled";
    internal const string CancelledReason = "Execution was cancelled";
    private const string TimedOutCode = "TimedOut";
    private const string ThresholdStoppedCode = "ThresholdStopped";

    private readonly IPipelineRunRequestStore _requests;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelineRequestProcessor> _logger;
    private readonly IExecutionCompletionSignaler? _completionSignaler;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineRequestProcessor"/>.
    /// </summary>
    /// <param name="requests">The store of the stored requests, from which a run that has ended is removed.</param>
    /// <param name="scopeFactory">The factory of the scope each run gets.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="completionSignaler">The signaler of a run that has ended, or null when the host has none.</param>
    public PipelineRequestProcessor(
        IPipelineRunRequestStore requests,
        IServiceScopeFactory scopeFactory,
        ILogger<PipelineRequestProcessor> logger,
        IExecutionCompletionSignaler? completionSignaler = null)
    {
        _requests = requests;
        _scopeFactory = scopeFactory;
        _logger = logger ?? NullLogger<PipelineRequestProcessor>.Instance;
        _completionSignaler = completionSignaler;
    }

    /// <inheritdoc />
    Task IPipelineRequestProcessor.ProcessRequest(PipelineExecutionRequest request, PipelineRunRefusal? refusal, PipelineRunHandle run, CancellationToken stoppingToken)
        => ProcessRequest(request, refusal, run, stoppingToken);

    internal async Task ProcessRequest(PipelineExecutionRequest request, PipelineRunRefusal? refusal, PipelineRunHandle run, CancellationToken stoppingToken)
    {
        EtlLog.ExecutionDequeued(_logger, request.PipelineName, request.ExecutionId);

        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            EtlLog.ExecutionScopeCreated(_logger, request.ExecutionId);

            EstablishSystemAuthenticationContext(scope.ServiceProvider, request);

            var executionTracker = scope.ServiceProvider.GetRequiredService<IExecutionTracker>();
            var pipelineProvider = scope.ServiceProvider
                .GetRequiredService<IEtlPipelineProvider>();
            var dataGateways = scope.ServiceProvider.GetRequiredService<IDataGatewayProvider>();
            var broadcaster = scope.ServiceProvider.GetRequiredService<IPipelineStatusBroadcaster>();

            var orgId = await ResolveOwningOrg(scope.ServiceProvider, request, stoppingToken).ConfigureAwait(false);

            var stoppedWithHost = false;
            var claimLeftToLapse = false;
            PipelineRunClaimHold? held = null;
            try
            {
                // Why before the run is recorded as running (FDW-6939): a run that waits for its claim is not running, and a person's cancel reaches it here as it does a waiting run. The one
                // place for a queued run and a run carried out at once alike; a host that does not run pipelines claims nothing.
                var claimed = await ClaimRun(scope.ServiceProvider, request, refusal, run, stoppingToken).ConfigureAwait(false);
                held = claimed.Hold;
                refusal = claimed.Refusal;
                PipelineRunClaimHold.Current = held;

                await TransitionToRunning(executionTracker, dataGateways, broadcaster, request, orgId, _logger, stoppingToken).ConfigureAwait(false);
                if (refusal is not null)
                {
                    await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                        request.ExecutionId, request.PipelineName, orgId,
                        false, refusal.Reason, refusal.Message, 0, 0, 0, 0).ConfigureAwait(false);
                    return;
                }

                // Why here and not at the top (FDW-3805): the limit counts from the moment the run starts running, and a refused run never does.
                using var timeLimit = PipelineRunTimeLimit.Arm(run, request, _logger);
                await RunPipeline(scope.ServiceProvider, executionTracker, pipelineProvider, dataGateways, broadcaster, request, orgId, run, held, stoppingToken).ConfigureAwait(false);
            }
            // Why a person's cancel (or a timeout) wins over the host's stop (FDW-3880): it was asked for first, so the run ends Cancelled with the records the flow closed for it, and its
            // request is forgotten; it is not left Interrupted to be started again.
            catch (OperationCanceledException ex) when (run.IsCancelled)
            {
                PipelineRunControlLog.RunCancelledObserved(_logger, ex, request.ExecutionId, request.PipelineName);
                await HandleCancellation(
                    executionTracker, dataGateways, broadcaster, request, orgId,
                    CodeOfStop(run), ReasonOfStop(run, request),
                    _completionSignaler, _logger).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Why not cancelled (FDW-3236): Cancelled means a person cancelled the run. A run the host's own stop cut off, after the time it was given
                // (FDW-3786), is marked Interrupted with the records under it, and keeps its stored request, so the next start finds it and applies the
                // retry or leave rule (FDW-3787). A crash leaves the run Running with the same request, and the next start treats it the same way.
                stoppedWithHost = true;
                PipelineRunRecoveryLog.RunStoppedWithHost(_logger, request.ExecutionId, request.PipelineName);
                claimLeftToLapse = !(await InterruptRun(scope.ServiceProvider, executionTracker, broadcaster, request, orgId).ConfigureAwait(false)).Settled;
            }
            catch (Exception ex)
            {
                var unhandled = EtlLog.ExecutionExceptionInBackground(_logger, ex, request.PipelineName, request.ExecutionId);

                // Why recorded too (FDW-3862): an unhandled exception is a failure of the Internal category (the logged message's own code), which a threshold counts and does not treat as transient.
                await RecordFailureKind(executionTracker, request, GenericResult.Failure(unhandled)).ConfigureAwait(false);
                await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                    request.ExecutionId, request.PipelineName, orgId,
                    false, "Exception", ex.Message, 0, 0, 0, 0).ConfigureAwait(false);
            }
            finally
            {
                // The claim is released when the run ends, which promotes the runs waiting behind it (FDW-6939).
                // Why not for a run its host stopped that could not be judged (FDW-7403): its claim is what the next start's sweep finds it by, so it is left to lapse, and its half-done rule is applied then.
                if (!claimLeftToLapse)
                {
                    await ReleaseClaim(held).ConfigureAwait(false);
                }

                PipelineRunClaimHold.Current = null;

                // Why at the end of the run and not at its start (FDW-3236): a run that ends keeps nothing, but a run that is cut off must still have its request.
                // A request that cannot be removed is logged by the store, and is removed when the host next reads the stored requests, since its run has ended.
                if (!stoppedWithHost)
                {
                    _ = await _requests.Forget(request.ExecutionId, CancellationToken.None).ConfigureAwait(false);

                    // Why after the run has ended (FDW-3864): the rule counts the runs including this one. A test run writes no history of its own to count, so it is not judged.
                    if (request.TestRowsPerSource is null)
                    {
                        _ = await scope.ServiceProvider.GetRequiredService<PipelineFailureReporter>().Report(request.PipelineName, request.ExecutionId, request.TenantId, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    // The durable claim of a run before it starts (FDW-6939): the claiming step of the run's own scope, waiting while another run holds a key. A claim that cannot be made refuses the run.
    private static async Task<ClaimedRun> ClaimRun(
        IServiceProvider services, PipelineExecutionRequest request, PipelineRunRefusal? refusal, PipelineRunHandle run, CancellationToken stoppingToken)
    {
        // A run already refused is not claimed, and a host that does not run pipelines claims nothing.
        if (refusal is not null || PipelineTypes.OperationalConnection is not { Length: > 0 } || services.GetService<PipelineRunClaiming>() is not { } claiming)
        {
            return new ClaimedRun(null, refusal);
        }

        // Why both tokens: a person's cancel and the host's stop each end the wait, as they end a run that waits for its place.
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(run.Token, stoppingToken);
        var acquired = await claiming.Acquire(request, waiting.Token).ConfigureAwait(false);
        return acquired.IsSuccess && acquired.Value is { } hold
            ? new ClaimedRun(hold, null)
            : new ClaimedRun(null, new PipelineRunRefusal("RunClaimRefused", string.Concat(acquired.CurrentMessage)));
    }

    private static async Task ReleaseClaim(PipelineRunClaimHold? held)
    {
        if (held is not null)
        {
            await held.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed record ClaimedRun(PipelineRunClaimHold? Hold, PipelineRunRefusal? Refusal);

    // Why SYSTEM here: this is the run's BOOKKEEPING context -- execution records, metrics, status and
    // the configuration reads that build the pipeline. The pipeline's own work never runs under it: it
    // runs as its tenant run, the pipeline's tenant plus the roles the tenant recorded for it (FDW-1087),
    // which RunPipeline establishes around Execute.
    //
    // Why the run's tenant rides on the system context: it never narrows what the run sees; it
    // decides the tenant the rows the run writes default to, through every tenant column's
    // DEFAULT. Without it a tenant's execution records and outputs would land in the default
    // tenant, readable by every tenant. A request with no tenant is work that belongs to none, and
    // its rows land in the default tenant -- the column default, not a choice made here.
    internal void EstablishSystemAuthenticationContext(IServiceProvider services, PipelineExecutionRequest request)
    {
        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = request.TenantId is { } tenantId
            ? new SystemAuthenticationContext(tenantId)
            : new SystemAuthenticationContext();
        EtlLog.ExecutionElevatedToSystemContext(_logger, request.ExecutionId, request.TenantId);
    }

    // The organization a run's live events are addressed by (FDW-4276). A read that fails is logged and the run's events go to no firehose group, as for a pipeline that
    // states no organization; it is no longer the same answer silently.
    internal async Task<Guid?> ResolveOwningOrg(
        IServiceProvider services,
        PipelineExecutionRequest request,
        CancellationToken ct)
    {
        var owner = await OwningOrg.Of(services, request.PipelineId, ct).ConfigureAwait(false);
        if (owner.IsFailure)
        {
            EtlLog.OwningOrgNotRead(_logger, request.PipelineId, request.ExecutionId, owner.CurrentMessage);
            return null;
        }

        return owner.Value;
    }

    private static async Task TransitionToRunning(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        ILogger logger,
        CancellationToken stoppingToken)
    {
        var toTriggered = await executionTracker.TransitionState(
            request.ExecutionId,
            ExecutionStateTypes.Triggered,
            "Pipeline execution triggered",
            actor: "EtlServer",
            stoppingToken).ConfigureAwait(false);

        if (toTriggered.IsSuccess)
        {
            var toInitialized = await executionTracker.TransitionState(
                request.ExecutionId,
                ExecutionStateTypes.Initialized,
                "Pipeline execution initializing",
                actor: "EtlServer",
                stoppingToken).ConfigureAwait(false);

            if (toInitialized.IsSuccess)
            {
                var toRunning = await executionTracker.TransitionState(
                    request.ExecutionId,
                    ExecutionStateTypes.Running,
                    "Pipeline execution started",
                    actor: "EtlServer",
                    stoppingToken).ConfigureAwait(false);
                if (toRunning.IsSuccess)
                {
                    await StampStarted(dataGateways, logger, request.ExecutionId, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    EtlLog.StateTransitionStepFailed(logger, request.ExecutionId, "Running", toRunning.CurrentMessage);
                }
            }
            else
            {
                EtlLog.StateTransitionStepFailed(logger, request.ExecutionId, "Initialized", toInitialized.CurrentMessage);
            }
        }
        else
        {
            EtlLog.StateTransitionStepFailed(logger, request.ExecutionId, "Triggered", toTriggered.CurrentMessage);
        }

        await broadcaster.BroadcastStatusChange(
            request.PipelineName, request.ExecutionId, "Running", orgId: orgId).ConfigureAwait(false);
    }

    // Why here and not at enqueue: the metrics row is written when the run is queued, and a queued run
    // has not started. StartedAt is the moment the run reached Running; the row's CreateDate is metadata.
    private static async Task StampStarted(
        IDataGatewayProvider dataGateways,
        ILogger logger,
        Guid executionId,
        CancellationToken stoppingToken)
    {
        // Reported the same way a failed update is, because the metrics row is non-fatal enrichment.
        var etlDataStore = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(etlDataStore))
        {
            EtlLog.MetricsUpdateFailed(
                logger,
                executionId,
                $"{nameof(PipelineTypes)}.{nameof(PipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
            return;
        }

        var updateCommand = Update.In<ExecutionStartRecord>(EtlContainerName)
            .DataStore(etlDataStore)
            .Path(EtlSchemaPath)
            .Where("Id", executionId)
            .Value(new ExecutionStartRecord
            {
                Status = ExecutionStateTypes.Running.Name,
                StartedAt = DateTimeOffset.UtcNow
            });

        var updateResult = await dataGateways.ByName(DataGatewayNames.Main).Execute<int>(updateCommand, stoppingToken).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            EtlLog.MetricsUpdateFailed(logger, executionId, updateResult.CurrentMessage);
        }
    }

    [ConventionOverride(MaxMethodLines = 95, MaxCyclomaticComplexity = 15)]  // One sequence of steps that each end the run with its reason: identity, pipeline, watermark read, execute, watermark write.
    private async Task RunPipeline(
        IServiceProvider services,
        IExecutionTracker executionTracker,
        IEtlPipelineProvider pipelineProvider,
        IDataGatewayProvider dataGateways,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        PipelineRunHandle run,
        PipelineRunClaimHold? held,
        CancellationToken stoppingToken)
    {
        var runAs = await ResolveRunIdentity(services, request, _logger, stoppingToken).ConfigureAwait(false);
        if (!runAs.IsSuccess || runAs.Value is null)
        {
            await RecordFailureKind(executionTracker, request, runAs).ConfigureAwait(false);
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "RunIdentityRefused", runAs.CurrentMessage, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        // By id: the pipeline the trigger resolved in its caller's view. A name is unique only per tenant.
        var pipelineResult = await pipelineProvider.Get(request.PipelineId, stoppingToken).ConfigureAwait(false);
        if (!pipelineResult.IsSuccess || pipelineResult.Value is null)
        {
            var errorMsg = pipelineResult.NoValueReason().Message;
            EtlLog.ExecutionFailedInBackground(_logger, request.PipelineName, request.ExecutionId, errorMsg);
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "PipelineNotFound", errorMsg, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        using var pipeline = pipelineResult.Value;

        // Why the start instant is taken here: it is RunStarted for the run's steps and, after a successful run, the watermark the next run
        // reads as LastSuccessfulRun, so the next run's window begins where this one's began.
        var startedAt = DateTimeOffset.UtcNow;
        var watermark = await ReadWatermark(dataGateways, request, stoppingToken).ConfigureAwait(false);
        if (watermark.IsFailure)
        {
            await RecordFailureKind(executionTracker, request, watermark).ConfigureAwait(false);
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "WatermarkReadFailed", watermark.CurrentMessage, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        var options = OptionsOf(request, startedAt, watermark.Value?.LastSuccessfulRun) with { PauseGate = run.Gate, StepLimit = Fenced(StepLimitOf(request, startedAt), held) };
        var executeResult = await ExecuteAs(services, runAs.Value, pipeline, options, run.Token).ConfigureAwait(false);

        // A run a person cancelled ends as cancelled, not as the failure its runner reports for a stopped run (FDW-2950). A run the host's own stop cut off is
        // not a failure either: it is passed up as a stop, so it stays Running for the next start to find (FDW-3236).
        if (executeResult.IsFailure && (run.IsCancelled || stoppingToken.IsCancellationRequested))
        {
            throw new OperationCanceledException(run.IsCancelled ? run.Token : stoppingToken);
        }

        if (executeResult.IsSuccess)
        {
            var metrics = executeResult.Value!;
            var recorded = await RecordWatermark(dataGateways, request, watermark.Value, startedAt, stoppingToken).ConfigureAwait(false);
            if (recorded.IsFailure)
            {
                await RecordFailureKind(executionTracker, request, recorded).ConfigureAwait(false);
                await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                    request.ExecutionId, request.PipelineName, orgId,
                    false, "WatermarkWriteFailed", recorded.CurrentMessage,
                    metrics.RecordsExtracted, metrics.RecordsLoaded, metrics.RecordsFailed,
                    (long)metrics.TotalDuration.TotalMilliseconds).ConfigureAwait(false);
                return;
            }

            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                true, "Success", null,
                metrics.RecordsExtracted,
                metrics.RecordsLoaded,
                metrics.RecordsFailed,
                (long)metrics.TotalDuration.TotalMilliseconds).ConfigureAwait(false);
        }
        else
        {
            var errorMsg = executeResult.FailureReason().Message;
            EtlLog.ExecutionFailedInBackground(_logger, request.PipelineName, request.ExecutionId, errorMsg);
            await RecordFailureKind(executionTracker, request, executeResult).ConfigureAwait(false);

            // Metrics may be partially populated even on failure; treat as 0 when absent.
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "ExecutionFailed", errorMsg, 0, 0, 0, 0).ConfigureAwait(false);
        }
    }

    // The pipeline's work acts as its run identity (its person, or its tenant run); everything around it stays bookkeeping, and the
    // bookkeeping context comes back whatever the work does.
    internal static async Task<IGenericResult<IEtlPipelineExecutionResult>> ExecuteAs(
        IServiceProvider services,
        IAuthenticationContext runAs,
        IEtlPipeline pipeline,
        PipelineExecutionOptions options,
        CancellationToken stoppingToken)
    {
        var accessor = services.GetRequiredService<IAuthenticationContextAccessor>();
        var bookkeeping = accessor.Current;
        accessor.Current = runAs;
        try
        {
            // The run carries the parameters the trigger or schedule supplied and the tenant it acts for: the watermark is
            // kept per pipeline per tenant (FDW-1109).
            // The run is the execution it was queued under, and a pipeline it starts acts with its roles (FDW-1381).
            return await pipeline.Execute(options, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            accessor.Current = bookkeeping;
        }
    }

    // The options a queued run executes with. A request that states a row cap per source is a test run: sources stop at the cap and no
    // destination is written (FDW-1676); a request that states none is a real run.
    // The limit the run's flow takes its steps from (FDW-6141): the request's step limit, counted with the time limit it carries (an at-once run states both); a run whose
    // pipeline states no step limit has none.
    // A run that holds a durable claim asks before each step whether it still does (FDW-6939); a run that holds none takes its steps from its own limit alone.
    private IFlowStepLimit? Fenced(IFlowStepLimit? own, PipelineRunClaimHold? held) =>
        held is null ? own : new PipelineRunClaimFence(own, held.Check, _logger);

    private PipelineRunStepLimit? StepLimitOf(PipelineExecutionRequest request, DateTimeOffset startedAt) =>
        request.Limits is { } carried
            ? new PipelineRunStepLimit(carried, request.PipelineName, _logger)
            : request.MaxSteps is { } steps && request.TimeoutSeconds is { } seconds
                ? new PipelineRunStepLimit(PipelineRunTimeAndStepLimits.Start(startedAt, seconds, steps), request.PipelineName, _logger)
                : null;

    internal static PipelineExecutionOptions OptionsOf(PipelineExecutionRequest request) => OptionsOf(request, null, null);

    // The same, for a run the host started: it states when the run started (RunStarted) and, when the pipeline has a successful run on record,
    // that run's start time as the LastSuccessfulRun parameter (FDW-1982). A LastSuccessfulRun the caller supplied is kept: it overrides the stored one.
    internal static PipelineExecutionOptions OptionsOf(PipelineExecutionRequest request, DateTimeOffset? startedAt, DateTimeOffset? lastSuccessfulRun)
    {
        var parameters = request.Parameters;
        if (lastSuccessfulRun is { } last && (parameters is null || !parameters.Keys.Any(key => string.Equals(key, RunFacts.LastSuccessfulRun, StringComparison.OrdinalIgnoreCase))))
        {
            var merged = parameters is null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
            merged[RunFacts.LastSuccessfulRun] = last;
            parameters = merged;
        }

        var options = PipelineExecutionOptions.Production with
        {
            Parameters = parameters,
            RunStarted = startedAt,
            TenantId = request.TenantId,
            ExecutionId = request.ExecutionId,
            RunRoles = request.RunRoles,
            PersonId = request.PersonId,
            ContinuesRun = request.ContinuesRun
        };
        return request.TestRowsPerSource is { } rows
            ? options with { IsTestMode = true, MaxRowsPerSource = rows, SkipDestinationWrites = true }
            : options;
    }

    // The pipeline's watermark row for the run's tenant, read once as the run starts. ResolveTenantRun has already refused a run with no tenant, so
    // a request without one is refused here too and never reads a row for a made-up tenant.
    private async Task<IGenericResult<EtlPipelineWatermarkRecord?>> ReadWatermark(
        IDataGatewayProvider dataGateways, PipelineExecutionRequest request, CancellationToken stoppingToken)
    {
        if (request.TenantId is not { } tenantId)
        {
            return GenericResult<EtlPipelineWatermarkRecord?>.Failure(
                EtlLog.ExecutionRefusedNoTenant(_logger, request.ExecutionId, request.PipelineName));
        }

        return await PipelineWatermarks.Read(
            dataGateways.ByName(DataGatewayNames.Main), PipelineTypes.OperationalConnection, request.PipelineId, tenantId, request.PipelineName, _logger, stoppingToken)
            .ConfigureAwait(false);
    }

    // A real run that succeeded moves the watermark to when it started. A test run writes nothing, so it never moves it.
    private async Task<IGenericResult> RecordWatermark(
        IDataGatewayProvider dataGateways, PipelineExecutionRequest request, EtlPipelineWatermarkRecord? existing, DateTimeOffset startedAt,
        CancellationToken stoppingToken)
    {
        if (request.TestRowsPerSource is not null)
        {
            return GenericResult.Success();
        }

        if (request.TenantId is not { } tenantId)
        {
            return GenericResult.Failure(EtlLog.ExecutionRefusedNoTenant(_logger, request.ExecutionId, request.PipelineName));
        }

        return await PipelineWatermarks.Record(
            dataGateways.ByName(DataGatewayNames.Main), PipelineTypes.OperationalConnection, existing, request.PipelineId, tenantId, startedAt, request.ExecutionId,
            request.PipelineName, _logger, stoppingToken).ConfigureAwait(false);
    }

    // The identity a run's work acts as. A run a person started acts as that person (FDW-6137): their live membership and permissions in the run's
    // tenant, narrowed to what the run's roles carry, so the connection reads only the person's rows. A run with no person acts as its tenant run.
    internal static async Task<IGenericResult<IAuthenticationContext>> ResolveRunIdentity(
        IServiceProvider services,
        PipelineExecutionRequest request,
        ILogger logger,
        CancellationToken stoppingToken)
    {
        // A cancelled run resolves no identity and reads nothing; the token is passed to every read below as well.
        if (stoppingToken.IsCancellationRequested)
        {
            return GenericResult<IAuthenticationContext>.Failure(
                RunPersonLog.RunIdentityCancelled(logger, request.ExecutionId, request.PipelineName));
        }

        if (request.PersonId is not { } personId)
        {
            var tenantRun = await ResolveTenantRun(services, request, logger, stoppingToken).ConfigureAwait(false);
            return tenantRun.IsSuccess && tenantRun.Value is { } run
                ? GenericResult<IAuthenticationContext>.Success(run)
                : tenantRun.ToNewResult<IAuthenticationContext>();
        }

        if (request.TenantId is not { } tenantId)
        {
            return GenericResult<IAuthenticationContext>.Failure(
                EtlLog.ExecutionRefusedNoTenant(logger, request.ExecutionId, request.PipelineName));
        }

        return await PipelineRunPerson.Resolve(services, request, personId, tenantId, logger, stoppingToken).ConfigureAwait(false);
    }

    // The identity a run with no person acts as (FDW-1087): the pipeline's tenant and the roles that tenant recorded
    // for the pipeline, resolved now so a role change applies to the next run. A run with no tenant, a
    // pipeline with no roles in it, or a recorded role that no longer exists is refused -- never run as
    // the system, never as someone else.
    internal static async Task<IGenericResult<TenantRunAuthenticationContext>> ResolveTenantRun(
        IServiceProvider services,
        PipelineExecutionRequest request,
        ILogger logger,
        CancellationToken stoppingToken)
    {
        if (request.TenantId is not { } tenantId)
        {
            return GenericResult<TenantRunAuthenticationContext>.Failure(
                EtlLog.ExecutionRefusedNoTenant(logger, request.ExecutionId, request.PipelineName));
        }

        // The roles are the run's own (FDW-1161): expanded now, so a role changed since the run was accepted applies to this run,
        // and a role that no longer exists refuses it.
        var granted = await services.GetRequiredService<IRoleGrants>()
            .Get(request.RunRoles, stoppingToken).ConfigureAwait(false);
        if (!granted.IsSuccess || granted.Value is null)
        {
            return GenericResult<TenantRunAuthenticationContext>.Failure(
                EtlLog.ExecutionRefusedRunRoles(logger, request.ExecutionId, request.PipelineName, granted.CurrentMessage));
        }

        EtlLog.ExecutionActingAsTenantRun(logger, request.ExecutionId, request.PipelineId, tenantId,
            string.Join(", ", request.RunRoles));
        return GenericResult<TenantRunAuthenticationContext>.Success(new TenantRunAuthenticationContext(
            request.PipelineId, request.PipelineName, tenantId, request.RunRoles, [.. granted.Value]));
    }

    // Why the kind of failure is recorded on the run (FDW-3862): a threshold counts the failures that are not transient, and reads them from the run, not from its text. A record that
    // cannot be written is logged and the run ends all the same.
    private async Task RecordFailureKind(IExecutionTracker executionTracker, PipelineExecutionRequest request, IGenericResult failure)
    {
        var kind = RunFailureClassification.Of(failure);
        var recorded = await executionTracker
            .RecordEvent(request.ExecutionId, RunFailureClassification.Event, kind.Describe(), kind.ToEventData(), cancellationToken: CancellationToken.None)
            .ConfigureAwait(false);
        if (recorded.IsFailure)
        {
            PipelineRunRecoveryLog.FailureKindNotRecorded(_logger, request.ExecutionId, recorded.CurrentMessage);
        }
    }

    // Ends a run the host stopped, by the run mode its own record states (FDW-7403). A run carried out at once keeps no stored request and is never started again, so it ends half done as its pipeline's
    // rule says, through the one judgment that ends a run whose host died (it writes the run's row and its records); a queued run keeps its stored request, its records are closed here, and the next
    // start decides what becomes of it. Anyone watching is told, unless the run could not be judged: it is then left open, and its claim lapses and brings it to the sweep.
    internal async Task<IAtOnceRunEnd> InterruptRun(
        IServiceProvider services, IExecutionTracker executionTracker, IPipelineStatusBroadcaster broadcaster, PipelineExecutionRequest request, Guid? orgId)
    {
        var ended = await EndRunCarriedOutAtOnce(services, request).ConfigureAwait(false);
        if (!ended.Settled)
        {
            return ended;
        }

        const string message = "The host stopped while the run was running";
        if (!ended.EndedHere)
        {
            await CloseQueuedRunWithHost(executionTracker, request, message).ConfigureAwait(false);
        }

        await broadcaster.BroadcastStatusChange(request.PipelineName, request.ExecutionId, "Interrupted", message, orgId).ConfigureAwait(false);
        _completionSignaler?.Signal(request.ExecutionId, false, "Interrupted");
        return ended;
    }

    // A run with no tenant cannot be a run carried out at once (it claims by its tenant), so it is a queued run, and the missing tenant is logged.
    private async Task<IAtOnceRunEnd> EndRunCarriedOutAtOnce(IServiceProvider services, PipelineExecutionRequest request)
    {
        if (request.TenantId is not { } tenant)
        {
            _ = PipelineRunRecoveryLog.RunHasNoTenant(_logger, request.ExecutionId, request.PipelineName);
            return AtOnceRunEnds.NotOpenAtOnce;
        }

        return await services.GetRequiredService<PipelineAtOnceRecovery>().EndStoppedRun(tenant, request.ExecutionId, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task CloseQueuedRunWithHost(IExecutionTracker executionTracker, PipelineExecutionRequest request, string message)
    {
        PipelineRunRecoveryLog.RunInterruptedByStop(_logger, request.ExecutionId, request.PipelineName, SettingDefinitions.MaxPipelineRunRestarts);
        var closed = await new OpenRunRecords(executionTracker, _logger)
            .Close(request.ExecutionId, ExecutionStateTypes.Interrupted, "ServerStopped", message, CancellationToken.None).ConfigureAwait(false);
        if (closed.IsFailure)
        {
            PipelineRunRecoveryLog.RecordNotClosed(_logger, request.ExecutionId, closed.CurrentMessage);
        }
    }

    // Why a stopped run is told apart in this order (FDW-3865): a time limit that passed is the run's own, a threshold that fired is a stated rule, and anything else is
    // a person's cancel. A threshold stop is recorded only when nothing had ended the run before it (PipelineRunHandle.StopForThreshold).
    private static string CodeOfStop(PipelineRunHandle run) => run.IsTimedOut ? TimedOutCode : run.IsThresholdStopped ? ThresholdStoppedCode : CancelledCode;

    private static string ReasonOfStop(PipelineRunHandle run, PipelineExecutionRequest request) =>
        run.IsTimedOut ? PipelineRunTimeLimit.Reason(request) : run.ThresholdReason is { } reason ? reason : CancelledReason;

    // Ends a run that was cancelled (or timed out, FDW-3784): the run and every open record under it are closed with the state the stop names, with the code and a message
    // that says how far it got, the run's row in the metrics table is closed the same way, and anyone watching it is told. A run that passed its time limit ends TimedOut
    // (FDW-4278) and any other stop ends Cancelled; a timed-out run is not restarted, since its request is not kept for a run in a state that has ended. The figures are read before the closing,
    // from the events its stages wrote when they finished; a run whose records cannot be read is closed with the reason alone and the failure is logged.
    internal static async Task HandleCancellation(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        string resultCode,
        string reason,
        IExecutionCompletionSignaler? completionSignaler,
        ILogger logger)
    {
        var tracking = await RunTrackingReader.Read(executionTracker, request.ExecutionId, CancellationToken.None).ConfigureAwait(false);
        if (tracking.IsFailure)
        {
            PipelineRunRecoveryLog.CancelledRunNotRead(logger, request.ExecutionId, tracking.CurrentMessage);
        }

        // A run whose records cannot be read is closed with the reason alone (logged above); its records are read only when the read succeeded.
        var read = tracking.IsSuccess ? tracking.Value : null;
        var message = read is { } records ? CancelledRunMessage.Of(reason, records) : reason;
        // Typed as the state contract: the two options are different classes, so a conditional between them has no common type.
        IExecutionStateType endState = string.Equals(resultCode, TimedOutCode, StringComparison.Ordinal) ? ExecutionStateTypes.TimedOut : ExecutionStateTypes.Cancelled;
        var closed = await new OpenRunRecords(executionTracker, logger)
            .Close(request.ExecutionId, endState, resultCode, message, CancellationToken.None).ConfigureAwait(false);
        if (closed.IsFailure)
        {
            EtlLog.CancellationCompleteFailed(logger, request.ExecutionId, closed.CurrentMessage);
        }

        await WriteMetricsRow(dataGateways, logger, request.ExecutionId, new ExecutionUpdateRecord
        {
            Status = endState.Name,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationMs = read?.RunTimeMs,
            ErrorMessage = message,
        }).ConfigureAwait(false);

        await broadcaster.BroadcastStatusChange(request.PipelineName, request.ExecutionId, endState.Name, message, orgId).ConfigureAwait(false);

        completionSignaler?.Signal(request.ExecutionId, false, resultCode);
    }

    // Writes the run's row in the metrics table. A host that names no operational store has no row to write, and a failed write is logged too: neither stops the end of the run (FDW-7033).
    private static async Task WriteMetricsRow(IDataGatewayProvider dataGateways, ILogger logger, Guid executionId, ExecutionUpdateRecord updateRecord)
    {
        // This method reports through EtlLog, so an unset connection is reported the same way a failed update is rather than being passed on as a null store name.
        var etlDataStore = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(etlDataStore))
        {
            EtlLog.MetricsUpdateFailed(
                logger,
                executionId,
                $"{nameof(PipelineTypes)}.{nameof(PipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
            return;
        }

        var updateCommand = Update.In<ExecutionUpdateRecord>(EtlContainerName)
            .DataStore(etlDataStore)
            .Path(EtlSchemaPath)
            .Where("Id", executionId)
            .Value(updateRecord);

        var updateResult = await dataGateways.ByName(DataGatewayNames.Main).Execute<int>(updateCommand, CancellationToken.None).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            EtlLog.MetricsUpdateFailed(logger, executionId, updateResult.CurrentMessage);
        }
    }

    // Why a run's own end writes nothing to a record that is already closed (FDW-6986): the recovery of half-done runs may have ended it first, with its own state and reason, and the run's end must
    // neither reopen it nor overwrite it, so the row ends the same whichever of the two comes last. A record that cannot be read is not taken as closed.
    private static async Task<bool> AlreadyClosed(IExecutionTracker executionTracker, Guid executionId, ILogger logger)
    {
        var existing = await executionTracker.GetItem(executionId, CancellationToken.None).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is not { } item || !item.State.IsTerminal)
        {
            return false;
        }

        _ = PipelineAtOnceRecoveryLog.RunAlreadyClosed(logger, executionId, item.State.Name);
        return true;
    }

    internal static async Task CompleteWithMetrics(
        IDataGatewayProvider dataGateways,
        IExecutionTracker executionTracker,
        IPipelineStatusBroadcaster broadcaster,
        IExecutionCompletionSignaler? completionSignaler,
        ILogger logger,
        Guid executionId,
        string pipelineName,
        Guid? orgId,
        bool success,
        string resultCode,
        string? resultMessage,
        long recordsExtracted,
        long recordsLoaded,
        long recordsFailed,
        long durationMs)
    {
        if (await AlreadyClosed(executionTracker, executionId, logger).ConfigureAwait(false))
        {
            return;
        }

        var updateRecord = new ExecutionUpdateRecord
        {
            Status = success ? "Succeeded" : "Failed",
            CompletedAt = DateTimeOffset.UtcNow,
            RecordsExtracted = recordsExtracted,
            RecordsLoaded = recordsLoaded,
            RecordsFailed = recordsFailed,
            DurationMs = durationMs,
            ErrorMessage = resultMessage
        };

        // Why the metrics row's outcome stops nothing (FDW-7033): it is the run's second record. A write that cannot be made (the host names no operational store) or that fails is logged with its code
        // by WriteMetricsRow, and the run's end is still recorded on its execution item, broadcast and signaled, so whoever waits for the run is not left waiting for ever.
        await WriteMetricsRow(dataGateways, logger, executionId, updateRecord).ConfigureAwait(false);

        var completeResult = await executionTracker.Complete(
            executionId, success, resultCode, resultMessage,
            CancellationToken.None).ConfigureAwait(false);
        if (!completeResult.IsSuccess)
        {
            EtlLog.CompletionRecordFailed(logger, executionId, completeResult.CurrentMessage);
        }

        await broadcaster.BroadcastCompletion(new PipelineExecutionComplete
        {
            PipelineName = pipelineName,
            ExecutionId = executionId,
            Success = success,
            Status = success ? "Succeeded" : "Failed",
            RecordsExtracted = (int)recordsExtracted,
            RecordsLoaded = (int)recordsLoaded,
            RecordsFailed = (int)recordsFailed,
            DurationMs = durationMs,
            ErrorMessage = resultMessage
        }, orgId).ConfigureAwait(false);

        completionSignaler?.Signal(executionId, success, resultMessage);
    }
}
