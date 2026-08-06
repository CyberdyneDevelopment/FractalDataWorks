using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Loads every pipeline for lineage by resolving through the composing
/// <see cref="PipelineServiceConfigurationProvider"/> — list headers, then per-header <c>Get(id)</c>
/// compose — rather than a flat single-table read that cannot see the engine-body linkage columns.
/// </summary>
/// <remarks>
/// Why N+1: <see cref="PipelineServiceConfigurationProvider"/>'s list overload
/// (<c>Get(CancellationToken)</c>) returns headers ONLY — it does not call <c>ComposeTypedBody</c>/
/// <c>ComposeChildren</c> (only <c>Get(name)</c>/<c>Get(id)</c> compose). The whole lineage graph is
/// cached for 5 minutes by the caller, which is what makes the per-pipeline compose acceptable. This
/// loader does NOT change list-<c>Get</c> semantics — that would alter <c>ListPipelinesEndpointBase</c>
/// behavior and per-request cost globally, which is out of scope here.
/// </remarks>
internal static class PipelineLineageLoader
{
    /// <summary>
    /// Loads and projects every pipeline aggregate for lineage. A header whose compose fails, or whose
    /// composed aggregate carries no engine body, renders NODE-ONLY (no fabricated linkage) with a
    /// Warning naming the gap — never throws, never blanks the whole list. NO FALLBACKS.
    /// </summary>
    /// <param name="provider">The composing pipeline header provider.</param>
    /// <param name="logger">Logger for verbose load/compose tracing.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<IReadOnlyList<PipelineLineageRecord>> Load(
        PipelineServiceConfigurationProvider provider,
        ILogger logger,
        CancellationToken ct)
    {
        var headersResult = await provider.Get(ct).ConfigureAwait(false);
        var headers = headersResult.IsSuccess ? headersResult.Value ?? [] : [];
        ApiEndpointLog.PipelineHeadersLoaded(logger, headers.Count);

        var records = new List<PipelineLineageRecord>(headers.Count);
        var nodeOnlyCount = 0;

        foreach (var header in headers)
        {
            ApiEndpointLog.ComposingPipelineAggregate(logger, header.Name, header.Id);
            var composedResult = await provider.Get(header.Id, ct).ConfigureAwait(false);

            if (!composedResult.IsSuccess)
            {
                ApiEndpointLog.PipelineAggregateComposeFailed(logger, header.Name, composedResult.CurrentMessage!);
                records.Add(NodeOnlyRecord(header));
                nodeOnlyCount++;
                continue;
            }

            if (composedResult.Value is null)
            {
                ApiEndpointLog.PipelineNodeOnlyNoBody(logger, header.Name);
                records.Add(NodeOnlyRecord(header));
                nodeOnlyCount++;
                continue;
            }

            var record = PipelineLineageProjection.From(composedResult.Value, logger);
            if (string.IsNullOrEmpty(record.SourceDataSet) && string.IsNullOrEmpty(record.DestinationDataSet) &&
                string.IsNullOrEmpty(record.SourceConnectionName) && string.IsNullOrEmpty(record.DestinationConnectionName))
                nodeOnlyCount++;

            records.Add(record);
        }

        ApiEndpointLog.PipelinesProjectedForLineage(logger, records.Count, nodeOnlyCount);
        return records;
    }

    // Why: a compose-failed or body-less header still renders as a graph NODE (Name/Id/ServiceOptionType),
    // just with no linkage-derived edges — the absence is visible via the Warning, never masked.
    private static PipelineLineageRecord NodeOnlyRecord(PipelineConfiguration header) => new()
    {
        Id = header.Id,
        Name = header.Name,
        ServiceOptionType = header.ServiceOptionType ?? string.Empty
    };
}
