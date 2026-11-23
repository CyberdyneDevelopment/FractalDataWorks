using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>
/// Reads and closes the records of a run whose claim lapsed (FDW-6986): the run's row in <c>etl.PipelineExecution</c> and its execution items. A run's end is written once: a run already closed is
/// never written again, so the reason a run ended with stays whichever of the sweep and the run's own end comes last.
/// </summary>
internal sealed class PipelineHalfDoneRunRecords : IHalfDoneRunRecords
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineExecution";
    private const string ResultCode = "PipelineRunEndedHalfDone";

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineHalfDoneRunRecords"/> class.</summary>
    /// <param name="scopes">Gives each call a scope of its own, as the request store does.</param>
    /// <param name="logger">The logger.</param>
    public PipelineHalfDoneRunRecords(IServiceScopeFactory scopes, ILogger<PipelineHalfDoneRunRecords>? logger = null)
    {
        _scopes = scopes;
        _logger = logger ?? NullLogger<PipelineHalfDoneRunRecords>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<HalfDoneRunRow?>> Read(Guid tenantId, Guid executionId, CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
                return GenericResult<HalfDoneRunRow?>.Failure(PipelineRunRecoveryLog.StoreNotNamed(_logger));

            var gateway = scope.ServiceProvider.GetRequiredService<IDataGatewayProvider>().ByName(DataGatewayNames.Main);
            var rows = await gateway.Execute<IEnumerable<EtlPipelineExecutionRecord>>(
                Query.From<EtlPipelineExecutionRecord>(store, PathName, ContainerName).Where("Id", executionId).Build(), cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure || rows.Value is null)
                return rows.ToNewResult<HalfDoneRunRow?>();

            if (rows.Value.FirstOrDefault() is not { } row)
                return GenericResult<HalfDoneRunRow?>.Success(null);

            var item = await scope.ServiceProvider.GetRequiredService<IExecutionTracker>().GetItem(executionId, cancellationToken).ConfigureAwait(false);
            return item.IsFailure || item.Value is null
                ? item.ToNewResult<HalfDoneRunRow?>()
                : GenericResult<HalfDoneRunRow?>.Success(new HalfDoneRunRow(row.Id, row.PipelineName, row.RunMode, !item.Value.State.IsTerminal));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Close(Guid tenantId, Guid executionId, string reason, CancellationToken cancellationToken = default)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            Elevate(scope.ServiceProvider, tenantId);
            if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
                return GenericResult.Failure(PipelineRunRecoveryLog.StoreNotNamed(_logger));

            var tracker = scope.ServiceProvider.GetRequiredService<IExecutionTracker>();
            var current = await tracker.GetItem(executionId, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
                return current;

            if (current.Value is null)
                return GenericResult.Failure(PipelineAtOnceRecoveryLog.RunRecordNotReadable(_logger, executionId, "the run has no execution item"));

            // Why nothing is written for a run that is already closed: the run's own end may have come first, and its state and reason stand.
            if (current.Value.State.IsTerminal)
                return GenericResult.Success();

            var gateway = scope.ServiceProvider.GetRequiredService<IDataGatewayProvider>().ByName(DataGatewayNames.Main);
            var updated = await gateway.Execute<int>(
                Update.In<ExecutionUpdateRecord>(ContainerName).DataStore(store).Path(PathName).Where("Id", executionId)
                    .Value(new ExecutionUpdateRecord { Status = ExecutionStateTypes.Interrupted.Name, CompletedAt = DateTimeOffset.UtcNow, ErrorMessage = reason }),
                cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
                return updated;

            return await new OpenRunRecords(tracker, _logger).Close(executionId, ExecutionStateTypes.Interrupted, ResultCode, reason, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void Elevate(IServiceProvider services, Guid tenantId)
    {
        var accessor = services.GetRequiredService<IAuthenticationContextAccessor>();
        if (accessor.Current is null)
        {
            accessor.Current = new SystemAuthenticationContext(tenantId);
        }
    }
}
