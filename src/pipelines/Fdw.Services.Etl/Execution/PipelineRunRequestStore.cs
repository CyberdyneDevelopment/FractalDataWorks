using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Keeps the request of every pipeline run the host accepts and has not started in etl.PipelineRunRequest, and reads the runs that were accepted and not
/// started back when the host starts (FDW-3216). The table is the engine's alone: the request holds the values callers give a run, which are never written
/// to a run's record or events, so no screen or API lists it. A run that is Scheduled or AwaitingApproval on its execution record is unstarted; the record,
/// not the row, says so, and a row whose run has moved on is stale and is removed, never started.
/// </summary>
public sealed class PipelineRunRequestStore : IPipelineRunRequestStore
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineRunRequest";
    private const string MetricsContainerName = "PipelineExecution";
    private const int PageSize = 100;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunRequestStore"/> class.</summary>
    /// <param name="scopes">Makes the scope each call works in, since the data gateways and the execution tracker are scoped.</param>
    /// <param name="logger">The logger.</param>
    public PipelineRunRequestStore(IServiceScopeFactory scopes, ILogger<PipelineRunRequestStore>? logger = null)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? NullLogger<PipelineRunRequestStore>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Record(PipelineExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId is not { } tenantId)
        {
            return GenericResult.Failure(PipelineRunRecoveryLog.RunHasNoTenant(_logger, request.ExecutionId, request.PipelineName));
        }

        var text = PipelineRunRequestJson.Write(request, _logger);
        if (text.IsFailure || text.Value is null)
        {
            return text;
        }

        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            var open = Open(scope.ServiceProvider);
            return open.IsFailure
                ? open
                : await Save(open.Value.Gateway, open.Value.Store, request.ExecutionId, tenantId, text.Value, 0, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RecordHeld(PipelineExecutionRequest request, DateTimeOffset heldUntil, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, request.TenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open;
            }

            var held = await open.Value.Gateway.Execute<int>(
                Update.In<EtlPipelineRunRequestHold>(ContainerName).DataStore(open.Value.Store).Path(PathName)
                    .Where("ExecutionId", request.ExecutionId)
                    .Value(new EtlPipelineRunRequestHold { HeldUntil = heldUntil }),
                cancellationToken).ConfigureAwait(false);
            return held.IsSuccess && held.Value > 0
                ? GenericResult.Success()
                : GenericResult.Failure(PipelineRunRecoveryLog.RequestNotWritten(_logger, request.ExecutionId, held.IsSuccess ? "the run has no stored request" : held.CurrentMessage));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Forget(Guid executionId, CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, null);
            var open = Open(scope.ServiceProvider);
            return open.IsFailure ? open : await Remove(open.Value.Gateway, open.Value.Store, executionId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineExecutionRequest>> StartAgain(InterruptedRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        var old = run.Request;
        if (old.TenantId is not { } tenantId)
        {
            return GenericResult<PipelineExecutionRequest>.Failure(PipelineRunRecoveryLog.RunHasNoTenant(_logger, old.ExecutionId, old.PipelineName));
        }

        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open.ToNewResult<PipelineExecutionRequest>();
            }

            var tracker = scope.ServiceProvider.GetRequiredService<IExecutionTracker>();
            var created = await tracker.CreateItem(
                ExecutionItemTypes.Job,
                $"{old.PipelineName}_{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}",
                tenantId,
                parentId: null,
                correlationId: Guid.NewGuid().ToString("N"),
                triggerSource: old.TriggerSource,
                parameters: new Dictionary<string, object?>(StringComparer.Ordinal) { ["PipelineName"] = old.PipelineName },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (created.IsFailure || created.Value is null)
            {
                return GenericResult<PipelineExecutionRequest>.Failure(PipelineRunRecoveryLog.StartAgainFailed(_logger, old.ExecutionId, created.CurrentMessage));
            }

            var again = new PipelineExecutionRequest
            {
                ExecutionId = created.Value.Id,
                PipelineId = old.PipelineId,
                PipelineName = old.PipelineName,
                TriggerSource = old.TriggerSource,
                ScheduleName = old.ScheduleName,
                TenantId = old.TenantId,
                RunRoles = old.RunRoles,
                Parameters = old.Parameters,
                TestRowsPerSource = old.TestRowsPerSource
            };
            await OpenMetrics(open.Value.Gateway, open.Value.Store, again, created.Value.State.Name, cancellationToken).ConfigureAwait(false);
            return await Replace(tracker, open.Value.Gateway, open.Value.Store, tenantId, run, again, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> GiveUp(InterruptedRun run, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, run.Request.TenantId);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open;
            }

            await CloseMetrics(open.Value.Gateway, open.Value.Store, run.Request.ExecutionId, reason, cancellationToken).ConfigureAwait(false);
            var closed = await scope.ServiceProvider.GetRequiredService<IExecutionTracker>()
                .Complete(run.Request.ExecutionId, false, "ServerStopped", reason, cancellationToken).ConfigureAwait(false);
            return closed.IsFailure
                ? GenericResult.Failure(PipelineRunRecoveryLog.RecordNotClosed(_logger, run.Request.ExecutionId, closed.CurrentMessage))
                : await Remove(open.Value.Gateway, open.Value.Store, run.Request.ExecutionId, cancellationToken).ConfigureAwait(false);
        }
    }

    // The metrics row the engine updates as the run goes (the trigger writes it when it accepts a run); a failed write is logged, not fatal, as it is there.
    private async Task OpenMetrics(IDataGateway gateway, string store, PipelineExecutionRequest request, string state, CancellationToken cancellationToken)
    {
        var inserted = await gateway.Execute<int>(
            Insert.Into<EtlPipelineExecutionRecord>(MetricsContainerName).DataStore(store).Path(PathName)
                .Value(new EtlPipelineExecutionRecord { Id = request.ExecutionId, PipelineName = request.PipelineName, ScheduleName = request.ScheduleName, Status = state }),
            cancellationToken).ConfigureAwait(false);
        if (inserted.IsFailure)
        {
            PipelineRunRecoveryLog.MetricsNotWritten(_logger, request.ExecutionId, inserted.CurrentMessage);
        }
    }

    // The cut-off run's metrics row is closed as failed with the reason, the way a run that fails ends.
    private async Task CloseMetrics(IDataGateway gateway, string store, Guid executionId, string reason, CancellationToken cancellationToken)
    {
        var updated = await gateway.Execute<int>(
            Update.In<ExecutionUpdateRecord>(MetricsContainerName).DataStore(store).Path(PathName).Where("Id", executionId)
                .Value(new ExecutionUpdateRecord { Status = "Failed", CompletedAt = DateTimeOffset.UtcNow, ErrorMessage = reason }),
            cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            PipelineRunRecoveryLog.MetricsNotWritten(_logger, executionId, updated.CurrentMessage);
        }
    }

    // Stores the new run's request with one more restart, closes the old run as failed naming the new one, and removes the old run's request. A new run whose
    // request cannot be stored is closed too, so no record of a run is left unstarted with nothing to start it from.
    private async Task<IGenericResult<PipelineExecutionRequest>> Replace(
        IExecutionTracker tracker, IDataGateway gateway, string store, Guid tenantId, InterruptedRun run, PipelineExecutionRequest again,
        CancellationToken cancellationToken)
    {
        var text = PipelineRunRequestJson.Write(again, _logger);
        var saved = text.IsFailure || text.Value is null
            ? text
            : await Save(gateway, store, again.ExecutionId, tenantId, text.Value, run.Restarts + 1, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            _ = await tracker.Complete(again.ExecutionId, false, "RunRequestNotStored", saved.CurrentMessage, cancellationToken).ConfigureAwait(false);
            return saved.ToNewResult<PipelineExecutionRequest>();
        }

        var reason = $"The server stopped while the run was running; started again as run {again.ExecutionId}";
        await CloseMetrics(gateway, store, run.Request.ExecutionId, reason, cancellationToken).ConfigureAwait(false);
        var closed = await tracker.Complete(run.Request.ExecutionId, false, "ServerStopped", reason, cancellationToken).ConfigureAwait(false);
        if (closed.IsFailure)
        {
            PipelineRunRecoveryLog.RecordNotClosed(_logger, run.Request.ExecutionId, closed.CurrentMessage);
        }

        _ = await Remove(gateway, store, run.Request.ExecutionId, cancellationToken).ConfigureAwait(false);
        return GenericResult<PipelineExecutionRequest>.Success(again);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineRunRecovery>> Recover(CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, null);
            var open = Open(scope.ServiceProvider);
            if (open.IsFailure)
            {
                return open.ToNewResult<PipelineRunRecovery>();
            }

            var tracker = scope.ServiceProvider.GetRequiredService<IExecutionTracker>();
            var rows = await Rows(open.Value.Gateway, open.Value.Store, cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure || rows.Value is null)
            {
                return rows.ToNewResult<PipelineRunRecovery>();
            }

            var unstarted = await Classify(tracker, open.Value.Gateway, open.Value.Store, rows.Value, cancellationToken).ConfigureAwait(false);
            if (unstarted.IsFailure || unstarted.Value is null)
            {
                return unstarted.ToNewResult<PipelineRunRecovery>();
            }

            var without = await WithoutRow(tracker, rows.Value.Select(row => row.ExecutionId).ToHashSet(), cancellationToken).ConfigureAwait(false);
            return without.IsFailure
                ? without.ToNewResult<PipelineRunRecovery>()
                : GenericResult<PipelineRunRecovery>.Success(
                    new PipelineRunRecovery(unstarted.Value.Waiting, unstarted.Value.Approvals, unstarted.Value.Interrupted, without.Value));
        }
    }

    private sealed record Unstarted(
        IReadOnlyList<PipelineExecutionRequest> Waiting, IReadOnlyList<PipelineExecutionRequest> Approvals, IReadOnlyList<InterruptedRun> Interrupted);

    // The gateway and the name of the operational store the table is in; a host that names none cannot keep or read a request.
    private IGenericResult<(IDataGateway Gateway, string Store)> Open(IServiceProvider services)
    {
        var store = EtlPipelineTypes.OperationalConnection;
        return string.IsNullOrWhiteSpace(store)
            ? GenericResult<(IDataGateway Gateway, string Store)>.Failure(PipelineRunRecoveryLog.StoreNotNamed(_logger))
            : GenericResult<(IDataGateway Gateway, string Store)>.Success((services.GetRequiredService<IDataGatewayProvider>().ByName("Main"), store));
    }

    // A run accepted a second time (one approved after it was held for approval) already has its row: the row's request is rewritten.
    private async Task<IGenericResult> Save(
        IDataGateway gateway, string store, Guid executionId, Guid tenantId, string text, int restarts, CancellationToken cancellationToken)
    {
        var inserted = await gateway.Execute<int>(
            Insert.Into<EtlPipelineRunRequestRecord>(ContainerName).DataStore(store).Path(PathName)
                .Value(new EtlPipelineRunRequestRecord { ExecutionId = executionId, TenantId = tenantId, Request = text, Restarts = restarts }),
            cancellationToken).ConfigureAwait(false);
        if (inserted.IsSuccess)
        {
            return GenericResult.Success();
        }

        var rewritten = await gateway.Execute<int>(
            Update.In<EtlPipelineRunRequestText>(ContainerName).DataStore(store).Path(PathName)
                .Where("ExecutionId", executionId)
                .Value(new EtlPipelineRunRequestText { Request = text }),
            cancellationToken).ConfigureAwait(false);
        return rewritten.IsSuccess && rewritten.Value > 0
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunRecoveryLog.RequestNotWritten(_logger, executionId, inserted.CurrentMessage));
    }

    private async Task<IGenericResult> Remove(IDataGateway gateway, string store, Guid executionId, CancellationToken cancellationToken)
    {
        var removed = await gateway.Execute<int>(
            Delete.From(ContainerName).DataStore(store).Path(PathName).Where("ExecutionId", executionId).Build(), cancellationToken).ConfigureAwait(false);
        return removed.IsSuccess
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunRecoveryLog.RequestNotRemoved(_logger, executionId, removed.CurrentMessage));
    }

    private static async Task<IGenericResult<IReadOnlyList<EtlPipelineRunRequestRecord>>> Rows(IDataGateway gateway, string store, CancellationToken cancellationToken)
    {
        var read = await gateway.Execute<IEnumerable<EtlPipelineRunRequestRecord>>(
            Query.From<EtlPipelineRunRequestRecord>(store, PathName, ContainerName).Build(), cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? read.ToNewResult<IReadOnlyList<EtlPipelineRunRequestRecord>>()
            : GenericResult<IReadOnlyList<EtlPipelineRunRequestRecord>>.Success([.. read.Value]);
    }

    // Splits the stored requests by what their run is now. A run that is Scheduled is waiting, one that is AwaitingApproval waits for approval, oldest first
    // by when the run was made; any other run has moved on, so its request is stale and removed. A run whose record cannot be read keeps its request.
    private async Task<IGenericResult<Unstarted>> Classify(
        IExecutionTracker tracker, IDataGateway gateway, string store, IReadOnlyList<EtlPipelineRunRequestRecord> rows, CancellationToken cancellationToken)
    {
        var found = new List<(EtlPipelineRunRequestRecord Row, IExecutionItem Item)>();
        foreach (var row in rows)
        {
            var item = await tracker.GetItem(row.ExecutionId, cancellationToken).ConfigureAwait(false);
            if (item.IsFailure || item.Value is null)
            {
                PipelineRunRecoveryLog.RunRecordUnreadable(_logger, row.ExecutionId, item.CurrentMessage);
            }
            else if (IsKept(item.Value))
            {
                found.Add((row, item.Value));
            }
            else
            {
                PipelineRunRecoveryLog.StaleRequestRemoved(_logger, row.ExecutionId, item.Value.State.Name);
                _ = await Remove(gateway, store, row.ExecutionId, cancellationToken).ConfigureAwait(false);
            }
        }

        var waiting = new List<PipelineExecutionRequest>();
        var approvals = new List<PipelineExecutionRequest>();
        var interrupted = new List<InterruptedRun>();
        foreach (var (row, item) in found.OrderBy(pair => pair.Item.CreatedAt))
        {
            var request = PipelineRunRequestJson.Read(row.ExecutionId, row.Request, row.HeldUntil, _logger);
            if (request.IsFailure || request.Value is null)
            {
                await CloseUnreadable(tracker, gateway, store, row.ExecutionId, request.CurrentMessage, cancellationToken).ConfigureAwait(false);
            }
            else if (Is(item, ExecutionStateTypes.AwaitingApproval))
            {
                approvals.Add(request.Value);
            }
            else if (Is(item, ExecutionStateTypes.Scheduled))
            {
                waiting.Add(request.Value);
            }
            else
            {
                interrupted.Add(new InterruptedRun(request.Value, row.Restarts));
            }
        }

        return GenericResult<Unstarted>.Success(new Unstarted(waiting, approvals, interrupted));
    }

    // A stored request is kept for a run that has not started (Scheduled, AwaitingApproval) and for one that was running when the host stopped (Running, or
    // on its way there, Triggered or Initialized, FDW-3236); a run in any other state has ended, and its request is stale.
    private static bool IsKept(IExecutionItem item) =>
        Is(item, ExecutionStateTypes.Scheduled) || Is(item, ExecutionStateTypes.AwaitingApproval)
        || Is(item, ExecutionStateTypes.Running) || Is(item, ExecutionStateTypes.Triggered) || Is(item, ExecutionStateTypes.Initialized);

    private static bool Is(IExecutionItem item, IExecutionStateType state) => string.Equals(item.State.Name, state.Name, StringComparison.Ordinal);

    // A request that cannot be read closes its run's record as failed, naming why, and is removed, so it does not come up again at every start.
    private async Task CloseUnreadable(
        IExecutionTracker tracker, IDataGateway gateway, string store, Guid executionId, string? reason, CancellationToken cancellationToken)
    {
        var closed = await tracker.Complete(executionId, false, "RunRequestUnreadable", reason, cancellationToken).ConfigureAwait(false);
        if (closed.IsFailure)
        {
            PipelineRunRecoveryLog.RecordNotClosed(_logger, executionId, closed.CurrentMessage);
            return;
        }

        _ = await Remove(gateway, store, executionId, cancellationToken).ConfigureAwait(false);
    }

    // How many records of unstarted runs have no stored request: runs accepted before requests were kept. They are left as they are.
    private static async Task<IGenericResult<int>> WithoutRow(IExecutionTracker tracker, HashSet<Guid> withRow, CancellationToken cancellationToken)
    {
        var without = 0;
        foreach (var state in new IExecutionStateType[] { ExecutionStateTypes.Scheduled, ExecutionStateTypes.AwaitingApproval })
        {
            for (var page = 1; ; page++)
            {
                var listed = await tracker.ListExecutions(page, PageSize, ExecutionItemTypes.Job, state, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (listed.IsFailure || listed.Value is null)
                {
                    return listed.ToNewResult<int>();
                }

                without += listed.Value.Items.Count(item => !withRow.Contains(item.Id));
                if (listed.Value.Items.Count < PageSize)
                {
                    break;
                }
            }
        }

        return GenericResult<int>.Success(without);
    }

    // The run's bookkeeping context, as the background service establishes it: the run's tenant rides on it so the row lands under the run's tenant.
    private static void Elevate(IServiceProvider services, Guid? tenantId)
    {
        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = tenantId is { } tenant ? new SystemAuthenticationContext(tenant) : new SystemAuthenticationContext();
    }
}
