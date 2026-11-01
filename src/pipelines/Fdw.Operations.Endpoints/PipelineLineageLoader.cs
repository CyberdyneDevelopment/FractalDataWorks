using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Loads every configured pipeline for lineage through <see cref="IPipelineConfigurationProvider"/>.
/// </summary>
/// <remarks>
/// Why whole: what a graph pipeline reads and writes is named in the settings of its components, so the list is read as records and
/// each graph is then read whole, as the workspace map reads it. A pipeline listed with its steps already is not read again.
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
        var configured = await provider.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (!configured.IsSuccess)
            return configured.ToNewResult<IReadOnlyList<PipelineLineageRecord>>();

        var pipelines = configured.Value!;
        ApiEndpointLog.PipelineHeadersLoaded(logger, pipelines.Count);

        var records = new List<PipelineLineageRecord>(pipelines.Count);
        var nodeOnlyCount = 0;

        foreach (var pipeline in pipelines)
        {
            var whole = await Whole(provider, pipeline, ct).ConfigureAwait(false);
            if (whole.IsFailure)
                return whole.ToNewResult<IReadOnlyList<PipelineLineageRecord>>();

            var record = PipelineLineageProjection.From(whole.Value!, logger);
            if (record.Uses.Count == 0)
                nodeOnlyCount++;

            records.Add(record);
        }

        ApiEndpointLog.PipelinesProjectedForLineage(logger, records.Count, nodeOnlyCount);
        return GenericResult<IReadOnlyList<PipelineLineageRecord>>.Success(records);
    }

    /// <summary>Reads every configured graph pipeline whole (FDW-2157), for what needs the graph and not only what it uses.</summary>
    /// <param name="provider">The pipeline domain provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The graph pipelines; a failed read is handed back, never an empty list.</returns>
    public static async Task<IGenericResult<IReadOnlyList<GraphPipelineConfiguration>>> ReadGraphs(
        IPipelineConfigurationProvider provider,
        CancellationToken ct)
    {
        var configured = await provider.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (!configured.IsSuccess)
            return configured.ToNewResult<IReadOnlyList<GraphPipelineConfiguration>>();

        var graphs = new List<GraphPipelineConfiguration>();
        foreach (var pipeline in configured.Value!)
        {
            var whole = await Whole(provider, pipeline, ct).ConfigureAwait(false);
            if (whole.IsFailure)
                return whole.ToNewResult<IReadOnlyList<GraphPipelineConfiguration>>();

            if (whole.Value is GraphPipelineConfiguration graph)
                graphs.Add(graph);
        }

        return GenericResult<IReadOnlyList<GraphPipelineConfiguration>>.Success(graphs);
    }

    private static async Task<IGenericResult<IPipelineImplementationConfiguration>> Whole(
        IPipelineConfigurationProvider provider,
        IPipelineImplementationConfiguration pipeline,
        CancellationToken ct)
    {
        if (pipeline is not GraphPipelineConfiguration graph || graph.Components.Count > 0)
            return GenericResult<IPipelineImplementationConfiguration>.Success(pipeline);

        return await provider.Get(pipeline.Id, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
    }
}
