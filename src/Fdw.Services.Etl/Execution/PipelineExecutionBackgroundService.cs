using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Conventions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Projects.Abstractions;
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
    private const string EtlContainerName = "PipelineExecution";
    private const string EtlSchemaPath = "etl";

    private readonly PipelineExecutionQueue _queue;
    private readonly PipelineRunSlot _slot;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelineExecutionBackgroundService> _logger;
    private readonly IExecutionCompletionSignaler? _completionSignaler;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionBackgroundService"/>.
    /// </summary>
    public PipelineExecutionBackgroundService(
        PipelineExecutionQueue queue,
        PipelineRunSlot slot,
        IServiceScopeFactory scopeFactory,
        ILogger<PipelineExecutionBackgroundService> logger,
        IExecutionCompletionSignaler? completionSignaler = null)
    {
        _queue = queue;
        _slot = slot ?? throw new ArgumentNullException(nameof(slot));
        _scopeFactory = scopeFactory;
        _logger = logger ?? NullLogger<PipelineExecutionBackgroundService>.Instance;
        _completionSignaler = completionSignaler;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EtlLog.BackgroundExecutorStarted(_logger);

        // Why each run on its own task: a run that waits for the pipeline it started has to leave the loop free to start that one.
        // Why the slot is taken here: runs still start in the order they were queued and never overlap (FDW-1511).
        List<Task> running = [];
        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await _slot.Acquire(stoppingToken).ConfigureAwait(false);
            running.RemoveAll(task => task.IsCompleted);
            running.Add(Task.Run(() => RunHoldingSlot(request, stoppingToken), CancellationToken.None));
        }

        await Task.WhenAll(running).ConfigureAwait(false);
        EtlLog.BackgroundExecutorStopping(_logger);
    }

    private async Task RunHoldingSlot(PipelineExecutionRequest request, CancellationToken stoppingToken)
    {
        _slot.MarkHeld();
        try
        {
            await ProcessRequest(request, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            _slot.Release();
        }
    }

    private async Task ProcessRequest(PipelineExecutionRequest request, CancellationToken stoppingToken)
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

            try
            {
                await TransitionToRunning(executionTracker, dataGateways, broadcaster, request, orgId, _logger, stoppingToken).ConfigureAwait(false);
                await RunPipeline(scope.ServiceProvider, executionTracker, pipelineProvider, dataGateways, broadcaster, request, orgId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                EtlLog.ExecutionCancelledDuringShutdown(_logger, ex, request.PipelineName, request.ExecutionId);
                await HandleCancellation(executionTracker, broadcaster, request, orgId, _completionSignaler, _logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                EtlLog.ExecutionExceptionInBackground(_logger, ex, request.PipelineName, request.ExecutionId);
                await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                    request.ExecutionId, request.PipelineName, orgId,
                    false, "Exception", ex.Message, 0, 0, 0, 0).ConfigureAwait(false);
            }
        }
    }

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

    private static async Task<Guid?> ResolveOwningOrg(
        IServiceProvider services,
        PipelineExecutionRequest request,
        CancellationToken ct)
    {
        var configResult = await services.GetRequiredService<IPipelineConfigurationProvider>().Get(request.PipelineId, ct).ConfigureAwait(false);
        return configResult.IsSuccess && configResult.Value is not null ? configResult.Value.OrgId : null;
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
        var etlDataStore = EtlPipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(etlDataStore))
        {
            EtlLog.MetricsUpdateFailed(
                logger,
                executionId,
                $"{nameof(EtlPipelineTypes)}.{nameof(EtlPipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
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

        var updateResult = await dataGateways.ByName("Main").Execute<int>(updateCommand, stoppingToken).ConfigureAwait(false);
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
        CancellationToken stoppingToken)
    {
        var runAs = await ResolveTenantRun(services, request, _logger, stoppingToken).ConfigureAwait(false);
        if (!runAs.IsSuccess || runAs.Value is null)
        {
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "RunIdentityRefused", runAs.CurrentMessage, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        // By id: the pipeline the trigger resolved in its caller's view. A name is unique only per tenant.
        var pipelineResult = await pipelineProvider.Get(request.PipelineId, stoppingToken).ConfigureAwait(false);
        if (!pipelineResult.IsSuccess || pipelineResult.Value is null)
        {
            var errorMsg = pipelineResult.CurrentMessage ?? $"Pipeline '{request.PipelineName}' not found";
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
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "WatermarkReadFailed", watermark.CurrentMessage, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        var options = OptionsOf(request, startedAt, watermark.Value?.LastSuccessfulRun);
        var executeResult = await ExecuteAs(services, runAs.Value, pipeline, options, stoppingToken).ConfigureAwait(false);
        if (executeResult.IsSuccess)
        {
            var metrics = executeResult.Value!;
            var recorded = await RecordWatermark(dataGateways, request, watermark.Value, startedAt, stoppingToken).ConfigureAwait(false);
            if (recorded.IsFailure)
            {
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
            var errorMsg = executeResult.CurrentMessage ?? "Pipeline execution failed";
            EtlLog.ExecutionFailedInBackground(_logger, request.PipelineName, request.ExecutionId, errorMsg);

            // Metrics may be partially populated even on failure; treat as 0 when absent.
            await CompleteWithMetrics(dataGateways, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "ExecutionFailed", errorMsg, 0, 0, 0, 0).ConfigureAwait(false);
        }
    }

    // The pipeline's work acts as its tenant run; everything around it stays bookkeeping, and the
    // bookkeeping context comes back whatever the work does.
    private static async Task<IGenericResult<IEtlPipelineExecutionResult>> ExecuteAs(
        IServiceProvider services,
        TenantRunAuthenticationContext runAs,
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
            RunRoles = request.RunRoles
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
            dataGateways.ByName("Main"), EtlPipelineTypes.OperationalConnection, request.PipelineId, tenantId, request.PipelineName, _logger, stoppingToken)
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
            dataGateways.ByName("Main"), EtlPipelineTypes.OperationalConnection, existing, request.PipelineId, tenantId, startedAt, request.ExecutionId,
            request.PipelineName, _logger, stoppingToken).ConfigureAwait(false);
    }

    // The identity a run's work acts as (FDW-1087): the pipeline's tenant and the roles that tenant recorded
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

    private static async Task HandleCancellation(
        IExecutionTracker executionTracker,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        IExecutionCompletionSignaler? completionSignaler,
        ILogger logger)
    {
        var completeResult = await executionTracker.Complete(
            request.ExecutionId, false, "Cancelled", "Execution was cancelled",
            CancellationToken.None).ConfigureAwait(false);
        if (!completeResult.IsSuccess)
        {
            EtlLog.CancellationCompleteFailed(logger, request.ExecutionId, completeResult.CurrentMessage);
        }

        await broadcaster.BroadcastStatusChange(
            request.PipelineName, request.ExecutionId, "Cancelled",
            "Execution was cancelled", orgId).ConfigureAwait(false);

        completionSignaler?.Signal(request.ExecutionId, false, "Cancelled");
    }

    private static async Task CompleteWithMetrics(
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

        // This method returns Task and reports through EtlLog, so an unset connection is reported the
        // same way a failed update is rather than being passed on as a null store name.
        var etlDataStore = EtlPipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(etlDataStore))
        {
            EtlLog.MetricsUpdateFailed(
                logger,
                executionId,
                $"{nameof(EtlPipelineTypes)}.{nameof(EtlPipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
            return;
        }

        var updateCommand = Update.In<ExecutionUpdateRecord>(EtlContainerName)
            .DataStore(etlDataStore)
            .Path(EtlSchemaPath)
            .Where("Id", executionId)
            .Value(updateRecord);

        var updateResult = await dataGateways.ByName("Main").Execute<int>(updateCommand, CancellationToken.None).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            EtlLog.MetricsUpdateFailed(logger, executionId, updateResult.CurrentMessage);
        }

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
