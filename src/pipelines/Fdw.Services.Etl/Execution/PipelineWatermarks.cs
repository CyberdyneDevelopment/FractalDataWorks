using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads and writes the last-successful watermark of a pipeline in the operational store (etl.PipelineWatermark) (FDW-1109).
/// </summary>
/// <remarks>
/// <para>
/// One row per pipeline per tenant. The engine reads it when a run starts, to offer it as <c>{{LastSuccessfulRun}}</c>, and
/// writes it only after a successful run's load has committed, with the time that run started: anything modified after the
/// run began is picked up by the next one. A failed run never reaches the write.
/// </para>
/// <para>
/// Runs of one pipeline can overlap (FDW-3117), so the write is safe for that: a run that finds the row already inserted by an
/// overlapping run updates it instead of failing, and an update only moves the watermark forward, so the run that finishes last cannot
/// move it back.
/// </para>
/// <para>
/// Why a static class over the gateway, as <c>PipelineExecutionBackgroundService</c> does for its execution rows: the
/// watermark is operational state written through the same store and path, and it needs no service of its own. The store
/// name is the host's, passed in; nothing here names a store.
/// </para>
/// </remarks>
public static class PipelineWatermarks
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineWatermark";

    /// <summary>Reads the pipeline's watermark row for a tenant.</summary>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="operationalStore">The operational store the host names.</param>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="pipelineName">The pipeline name, for messages.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The row, or <see langword="null"/> as the value when the pipeline has no successful run on record.</returns>
    public static async Task<IGenericResult<EtlPipelineWatermarkRecord?>> Read(
        IDataGateway gateway,
        string? operationalStore,
        Guid pipelineId,
        Guid tenantId,
        string pipelineName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operationalStore))
            return GenericResult<EtlPipelineWatermarkRecord?>.Failure(PipelineRunLog.WatermarkStoreNotNamed(logger));

        var read = await gateway.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(
            Query.From<EtlPipelineWatermarkRecord>(operationalStore, PathName, ContainerName)
                .Where(r => r.PipelineId).Equal(pipelineId)
                .Where(r => r.TenantId).Equal(tenantId)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsFailure
            ? GenericResult<EtlPipelineWatermarkRecord?>.Failure(PipelineRunLog.WatermarkReadFailed(logger, pipelineName, LoadFailureText.Of(read)))
            : GenericResult<EtlPipelineWatermarkRecord?>.Success(read.Value!.FirstOrDefault());
    }

    /// <summary>Records that a run succeeded: inserts the pipeline's row, or moves the existing one forward and never back.</summary>
    /// <param name="gateway">The data gateway.</param>
    /// <param name="operationalStore">The operational store the host names.</param>
    /// <param name="existing">The row <see cref="Read"/> returned, or <see langword="null"/> when there was none.</param>
    /// <param name="pipelineId">The pipeline.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="startedAt">When the successful run started.</param>
    /// <param name="executionId">The successful run.</param>
    /// <param name="pipelineName">The pipeline name, for messages.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or a failure saying the data loaded but the watermark did not move.</returns>
    public static async Task<IGenericResult> Record(
        IDataGateway gateway,
        string? operationalStore,
        EtlPipelineWatermarkRecord? existing,
        Guid pipelineId,
        Guid tenantId,
        DateTimeOffset startedAt,
        Guid executionId,
        string pipelineName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operationalStore))
            return GenericResult.Failure(PipelineRunLog.WatermarkStoreNotNamed(logger));

        var row = existing;
        if (row is null)
        {
            var inserted = await gateway.Execute<int>(
                Insert.Into<EtlPipelineWatermarkRecord>(ContainerName).DataStore(operationalStore).Path(PathName)
                    .Value(Row(Guid.CreateVersion7(), pipelineId, tenantId, startedAt, executionId)),
                cancellationToken).ConfigureAwait(false);
            if (inserted.IsSuccess)
            {
                PipelineRunLog.WatermarkAdvanced(logger, pipelineName, Stamp(startedAt));
                return GenericResult.Success();
            }

            // A run of the same pipeline that overlapped this one may have inserted the row first. The row says so, not the text of the error.
            var again = await Read(gateway, operationalStore, pipelineId, tenantId, pipelineName, logger, cancellationToken).ConfigureAwait(false);
            if (again.IsFailure || again.Value is null)
                return GenericResult.Failure(PipelineRunLog.WatermarkWriteFailed(logger, pipelineName, LoadFailureText.Of(inserted)));

            row = again.Value;
        }

        if (row.LastSuccessfulRun >= startedAt)
        {
            PipelineRunLog.WatermarkAlreadyAhead(logger, pipelineName);
            return GenericResult.Success();
        }

        // Conditional as well, because a later run can move the row between the read and this write; the condition keeps it from going back.
        var moved = await gateway.Execute<int>(
            Update.In<EtlPipelineWatermarkRecord>(ContainerName).DataStore(operationalStore).Path(PathName)
                .Where("Id", row.Id)
                .Where("LastSuccessfulRun", new LessThanOperator(), startedAt)
                .Value(Row(row.Id, pipelineId, tenantId, startedAt, executionId)),
            cancellationToken).ConfigureAwait(false);
        if (moved.IsFailure)
            return GenericResult.Failure(PipelineRunLog.WatermarkWriteFailed(logger, pipelineName, LoadFailureText.Of(moved)));

        PipelineRunLog.WatermarkAdvanced(logger, pipelineName, Stamp(startedAt));
        return GenericResult.Success();
    }

    private static EtlPipelineWatermarkRecord Row(Guid id, Guid pipelineId, Guid tenantId, DateTimeOffset startedAt, Guid executionId) => new()
    {
        Id = id,
        PipelineId = pipelineId,
        TenantId = tenantId,
        LastSuccessfulRun = startedAt,
        ExecutionId = executionId,
    };

    private static string Stamp(DateTimeOffset startedAt) => startedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
