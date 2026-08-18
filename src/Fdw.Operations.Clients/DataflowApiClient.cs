namespace Fdw.Operations.Clients;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for dataflow, lineage, and impact analysis endpoints.
/// </summary>
public class DataflowApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataflowApiClient"/> class.
    /// </summary>
    public DataflowApiClient(HttpClient httpClient, ILogger<DataflowApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the complete dataflow graph.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the dataflow graph data.</returns>
    public virtual Task<IGenericResult<DataflowGraphPayload>> GetGraph(CancellationToken ct = default)
        => Get<DataflowGraphPayload>("dataflow/graph", ct);

    /// <summary>
    /// Gets lineage information for a specific DataSet.
    /// </summary>
    /// <param name="datasetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the lineage results.</returns>
    public virtual Task<IGenericResult<DataSetLineagePayload>> GetLineage(string datasetName, CancellationToken ct = default)
        => Get<DataSetLineagePayload>($"dataflow/lineage/{Uri.EscapeDataString(datasetName)}", ct);

    /// <summary>
    /// Performs impact analysis for a specific target.
    /// </summary>
    /// <param name="targetType">The type of target (e.g., "Connection", "DataStore").</param>
    /// <param name="targetName">The name of the target.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the impact analysis results.</returns>
    // Why POST with a body rather than the path form this used to send: the endpoint is
    // POST /dataflow/impact taking an ImpactAnalysisRequest. The GET path variant it called
    // (dataflow/impact/{type}/{name}) is served by nothing, so impact analysis always 404'd.
    public virtual Task<IGenericResult<ImpactAnalysisPayload>> AnalyzeImpact(string targetType, string targetName, CancellationToken ct = default)
        => Post<ImpactAnalysisRequestPayload, ImpactAnalysisPayload>(
            "dataflow/impact",
            new ImpactAnalysisRequestPayload { TargetType = targetType, TargetName = targetName },
            ct);
}
