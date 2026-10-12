using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Loads every configured pipeline for lineage through <see cref="IPipelineConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// One read: the domain's list overload dispatches each row to the implementation provider it names,
/// so what comes back already carries the engine linkage the lineage graph needs. The per-pipeline
/// compose this loader used to do — list headers, then <c>Get(id)</c> each — was the shape of the
/// header/typed-body chain, and there is no header to compose from any more.
/// </remarks>
internal static class PipelineLineageLoader
{
    /// <summary>
    /// Loads and projects every configured pipeline for lineage. A pipeline carrying no ETL linkage
    /// renders NODE-ONLY (no fabricated linkage) with a Warning naming the gap; a failed read yields
    /// no records rather than a blanked graph. NO FALLBACKS.
    /// </summary>
    /// <param name="provider">The pipeline domain provider.</param>
    /// <param name="logger">Logger for verbose load tracing.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<IReadOnlyList<PipelineLineageRecord>> Load(
        IPipelineConfigurationProvider provider,
        ILogger logger,
        CancellationToken ct)
    {
        var read = await Read(provider, logger, ct).ConfigureAwait(false);
        if (!read.IsSuccess)
        {
            ApiEndpointLog.PipelineAggregateComposeFailed(logger, "(all)", read.CurrentMessage!);
            return [];
        }

        return read.Value!;
    }

    /// <summary>
    /// Reads and projects every configured pipeline, handing a failed read back to the caller.
    /// </summary>
    /// <remarks>
    /// For a caller that roots a graph at the pipelines: an empty list from a failed read would
    /// read as "no pipelines exist" and blank the graph silently, so the failure travels instead.
    /// </remarks>
    /// <param name="provider">The pipeline domain provider.</param>
    /// <param name="logger">Logger for verbose load tracing.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<IGenericResult<IReadOnlyList<PipelineLineageRecord>>> Read(
        IPipelineConfigurationProvider provider,
        ILogger logger,
        CancellationToken ct)
    {
        var configured = await provider.Get(ct).ConfigureAwait(false);
        if (!configured.IsSuccess)
            return configured.ToNewResult<IReadOnlyList<PipelineLineageRecord>>();

        var pipelines = configured.Value!;
        ApiEndpointLog.PipelineHeadersLoaded(logger, pipelines.Count);

        var records = new List<PipelineLineageRecord>(pipelines.Count);
        var nodeOnlyCount = 0;

        foreach (var pipeline in pipelines)
        {
            var record = PipelineLineageProjection.From(pipeline, logger);
            if (string.IsNullOrEmpty(record.SourceDataSet) && string.IsNullOrEmpty(record.DestinationDataSet) &&
                string.IsNullOrEmpty(record.SourceConnectionName) && string.IsNullOrEmpty(record.DestinationConnectionName))
                nodeOnlyCount++;

            records.Add(record);
        }

        ApiEndpointLog.PipelinesProjectedForLineage(logger, records.Count, nodeOnlyCount);
        return GenericResult<IReadOnlyList<PipelineLineageRecord>>.Success(records);
    }
}
