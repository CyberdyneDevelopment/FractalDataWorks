using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
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
    // Why OpsDb: the etl.PipelineExecution container lives in OpsDb (not "EtlDb").
    private const string EtlDataStore = "OpsDb";
    private const string EtlSchemaPath = "etl";

    private readonly PipelineExecutionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelineExecutionBackgroundService> _logger;
    // Why optional: PipelineExecutionBackgroundService lives in Services.Etl which ships
    // independently. When Services.Etl.Projects is registered, the signaler is present and
    // pipelines enqueued by the project orchestrator get their completion signaled.
    // When Services.Etl.Projects is not registered, the field is null and nothing changes.
    private readonly IExecutionCompletionSignaler? _completionSignaler;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineExecutionBackgroundService"/>.
    /// </summary>
    public PipelineExecutionBackgroundService(
        PipelineExecutionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PipelineExecutionBackgroundService> logger,
        IExecutionCompletionSignaler? completionSignaler = null)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        // Why NullLogger fallback: per FDW convention, ensures the service remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<PipelineExecutionBackgroundService>.Instance;
        _completionSignaler = completionSignaler;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EtlLog.BackgroundExecutorStarted(_logger);

        // Why ReadAllAsync with ConfigureAwait: respects the stoppingToken — when the host
        // shuts down, the loop exits after the current item completes. This is the graceful
        // shutdown mechanism that Task.Run lacked. In-flight work is not abandoned.
        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProcessRequest(request, stoppingToken).ConfigureAwait(false);
        }

        EtlLog.BackgroundExecutorStopping(_logger);
    }

    private async Task ProcessRequest(PipelineExecutionRequest request, CancellationToken stoppingToken)
    {
        EtlLog.ExecutionDequeued(_logger, request.PipelineName, request.ExecutionId);

        // Why CreateAsyncScope: each execution needs its own DI scope so scoped services
        // (IExecutionTracker, IDataGateway) have correct lifetimes. CreateAsyncScope supports
        // async disposal of scoped services.
        // Why ConfigureAwait(false) on the await using block: DisposeAsync on the scope does not
        // need to resume on the original SynchronizationContext.
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            EtlLog.ExecutionScopeCreated(_logger, request.ExecutionId);

            // Why here, before any DataGateway/connection use in this scope: a background execution has
            // no ClaimsPrincipal, so without this the scoped IAuthenticationContext stays absent, RLS
            // SESSION_CONTEXT is never set, and the run reads/writes across every tenant (system-bypass).
            // WorkAuthenticationContext carries the execution's own TenantId instead of a token claim.
            // Only set when the scope has no caller-established context yet (Current is null) — a
            // background execution never has one, but this keeps the check explicit and safe if this
            // scope is ever reused for a caller-attributed flow.
            EstablishWorkAuthenticationContext(scope.ServiceProvider, request);

            var executionTracker = scope.ServiceProvider.GetRequiredService<IExecutionTracker>();
            var pipelineProvider = scope.ServiceProvider
                .GetRequiredService<IPlatformServiceProvider<IEtlPipeline, PipelineConfiguration>>();
            var dataGateway = scope.ServiceProvider.GetRequiredService<IDataGateway>();
            var broadcaster = scope.ServiceProvider.GetRequiredService<IPipelineStatusBroadcaster>();

            // Why: the realtime firehose is scoped to the pipeline's OWNING org (org:{OrgId}:
            // pipeline-updates). Resolve it once per execution from the parent PipelineConfiguration
            // and thread it to every lifecycle broadcast. An unresolvable config yields null org —
            // no org firehose (there is no global cross-org group), never a fallback.
            var orgId = await ResolveOwningOrg(scope.ServiceProvider, request, stoppingToken).ConfigureAwait(false);

            try
            {
                await TransitionToRunning(executionTracker, broadcaster, request, orgId, _logger, stoppingToken).ConfigureAwait(false);
                await RunPipeline(executionTracker, pipelineProvider, dataGateway, broadcaster, request, orgId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                EtlLog.ExecutionCancelledDuringShutdown(_logger, ex, request.PipelineName, request.ExecutionId);
                await HandleCancellation(executionTracker, broadcaster, request, orgId, _completionSignaler, _logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                EtlLog.ExecutionExceptionInBackground(_logger, ex, request.PipelineName, request.ExecutionId);
                // Why CancellationToken.None: unhandled exceptions must still record failure state.
                await CompleteWithMetrics(dataGateway, executionTracker, broadcaster, _completionSignaler, _logger,
                    request.ExecutionId, request.PipelineName, orgId,
                    false, "Exception", ex.Message, 0, 0, 0, 0).ConfigureAwait(false);
            }
        }
    }

    // Why: stamps the per-execution scope's ambient IAuthenticationContext (via the AsyncLocal-backed
    // IAuthenticationContextAccessor) with a WorkAuthenticationContext carrying request.TenantId, so
    // every MsSqlConnection created for the rest of this scope sets RLS SESSION_CONTEXT('TenantId').
    // No-op when the accessor isn't registered (Connections.MsSql not loaded), the request carries no
    // TenantId, or the scope already has a caller-established context.
    // Why internal (not private): unit-tested directly by Fdw.Services.Etl.Tests via
    // InternalsVisibleTo, rather than standing up the full ProcessRequest dependency graph.
    internal void EstablishWorkAuthenticationContext(IServiceProvider services, PipelineExecutionRequest request)
    {
        if (!request.TenantId.HasValue)
        {
            return;
        }

        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = new WorkAuthenticationContext(request.TenantId.Value);
        EtlLog.WorkAuthenticationContextEstablished(_logger, request.ExecutionId, request.TenantId.Value);
    }

    // Why: the owning org for firehose scoping comes from the parent PipelineConfiguration
    // (pipe.Pipeline.OrgId). Resolved through the standard config provider (cached), so it is not an
    // extra uncached DB hit per execution. A missing/unresolvable config yields null — no org firehose.
    private static async Task<Guid?> ResolveOwningOrg(
        IServiceProvider services,
        PipelineExecutionRequest request,
        CancellationToken ct)
    {
        var configProvider = services.GetRequiredService<IServiceConfigurationProvider<PipelineConfiguration>>();
        var configResult = await configProvider.Get(request.PipelineName, ct).ConfigureAwait(false);
        return configResult.IsSuccess && configResult.Value is not null ? configResult.Value.OrgId : null;
    }

    private static async Task TransitionToRunning(
        IExecutionTracker executionTracker,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        ILogger logger,
        CancellationToken stoppingToken)
    {
        // Why full walk: the state machine only allows Scheduled→Triggered, Triggered→Initialized,
        // Initialized→Running; a direct Scheduled→Running jump is rejected. Each step failure is
        // logged as Warning and stops the walk — execution proceeds regardless (non-fatal lifecycle).
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
                if (!toRunning.IsSuccess)
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

    private async Task RunPipeline(
        IExecutionTracker executionTracker,
        IPlatformServiceProvider<IEtlPipeline, PipelineConfiguration> pipelineProvider,
        IDataGateway dataGateway,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        CancellationToken stoppingToken)
    {
        // Resolve pipeline from provider
        var pipelineResult = await pipelineProvider.Get(request.PipelineName, stoppingToken).ConfigureAwait(false);
        if (!pipelineResult.IsSuccess || pipelineResult.Value is null)
        {
            var errorMsg = pipelineResult.CurrentMessage ?? $"Pipeline '{request.PipelineName}' not found";
            EtlLog.ExecutionFailedInBackground(_logger, request.PipelineName, request.ExecutionId, errorMsg);
            await CompleteWithMetrics(dataGateway, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "PipelineNotFound", errorMsg, 0, 0, 0, 0).ConfigureAwait(false);
            return;
        }

        using var pipeline = pipelineResult.Value;
        var executeResult = await pipeline.Execute(stoppingToken).ConfigureAwait(false);

        if (executeResult.IsSuccess)
        {
            var metrics = executeResult.Value!;

            await CompleteWithMetrics(dataGateway, executionTracker, broadcaster, _completionSignaler, _logger,
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
            await CompleteWithMetrics(dataGateway, executionTracker, broadcaster, _completionSignaler, _logger,
                request.ExecutionId, request.PipelineName, orgId,
                false, "ExecutionFailed", errorMsg, 0, 0, 0, 0).ConfigureAwait(false);
        }
    }

    private static async Task HandleCancellation(
        IExecutionTracker executionTracker,
        IPipelineStatusBroadcaster broadcaster,
        PipelineExecutionRequest request,
        Guid? orgId,
        IExecutionCompletionSignaler? completionSignaler,
        ILogger logger)
    {
        // Why CancellationToken.None: the stopping token is already cancelled. We still need
        // to record the cancellation state; using the stopping token would prevent cleanup writes.
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

        // Why: signal completion even for cancellation so the waiting orchestrator is unblocked.
        completionSignaler?.Signal(request.ExecutionId, false, "Cancelled");
    }

    private static async Task CompleteWithMetrics(
        IDataGateway dataGateway,
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

        var updateCommand = Update.In<ExecutionUpdateRecord>(EtlContainerName)
            .DataStore(EtlDataStore)
            .Path(EtlSchemaPath)
            .Where("Id", executionId)
            .Value(updateRecord);

        // Why CancellationToken.None: completion writes must succeed even during shutdown.
        var updateResult = await dataGateway.Execute<int>(updateCommand, CancellationToken.None).ConfigureAwait(false);
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

        // Why: signal AFTER BroadcastCompletion so the orchestrator only proceeds after the
        // broadcast has been sent — consistent ordering of observable effects.
        completionSignaler?.Signal(executionId, success, resultMessage);
    }
}
