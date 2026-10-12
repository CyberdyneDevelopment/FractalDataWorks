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
/// API client for Data Lineage operations.
/// </summary>
public class LineageApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageApiClient"/> class.
    /// </summary>
    public LineageApiClient(HttpClient httpClient, ILogger<LineageApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the lineage graph for an entity.
    /// </summary>
    /// <param name="entityType">The entity type (DataSet, DataStore, Pipeline, Connection).</param>
    /// <param name="entityName">The entity name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the lineage graph.</returns>
    public virtual Task<IGenericResult<LineageGraphPayload>> GetLineage(string entityType, string entityName, CancellationToken ct = default)
        => Get<LineageGraphPayload>($"lineage/{entityType}/{entityName}", ct);

    /// <summary>
    /// Gets the column-level lineage for a specific field.
    /// </summary>
    /// <param name="entityType">The entity type (DataSet, DataStore).</param>
    /// <param name="entityName">The entity name.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the field-level lineage graph.</returns>
    public virtual Task<IGenericResult<LineageGraphPayload>> GetColumnLineage(string entityType, string entityName, string fieldName, CancellationToken ct = default)
        => Get<LineageGraphPayload>($"lineage/{entityType}/{entityName}/fields/{fieldName}", ct);

    /// <summary>
    /// Gets the downstream impact analysis for an entity.
    /// </summary>
    /// <param name="entityName">The entity name to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the impact analysis.</returns>
    public virtual Task<IGenericResult<ImpactAnalysisPayload>> GetImpact(string entityName, CancellationToken ct = default)
        => Get<ImpactAnalysisPayload>($"lineage/{entityName}/impact", ct);

    /// <summary>
    /// Expands a single lineage node, returning its direct upstream and downstream neighbors.
    /// Used for lazy tree expansion in the lineage UI without loading the full transitive graph.
    /// </summary>
    /// <param name="nodeType">The type of the node (e.g., Project, Stage, Step, Pipeline, DataSet).</param>
    /// <param name="nodeId">The name-based identifier of the node to expand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the direct-neighbor subgraph for the specified node.</returns>
    public virtual Task<IGenericResult<LineageGraphPayload>> ExpandLineageNode(string nodeType, string nodeId, CancellationToken ct = default)
        => Get<LineageGraphPayload>($"etl/lineage/expand?nodeType={Uri.EscapeDataString(nodeType)}&nodeId={Uri.EscapeDataString(nodeId)}", ct);
}
