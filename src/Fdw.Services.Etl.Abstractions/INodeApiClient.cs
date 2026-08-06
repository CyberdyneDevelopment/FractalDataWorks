using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// Defines the API client contract for generic orchestration node endpoints.
/// These endpoints operate on the unified <c>pipe.OrchestrationNode</c> table
/// and are type-discriminated by <see cref="OrchestrationNodeConfiguration.NodeTypeId"/>.
/// </summary>
public interface INodeApiClient
{
    /// <summary>
    /// Lists all root orchestration nodes (nodes with no parent).
    /// </summary>
    Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> ListRootNodes(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single orchestration node by its logical identifier.
    /// </summary>
    Task<IGenericResult<OrchestrationNodeConfiguration>> GetNode(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single orchestration node with its full child subtree expanded to the requested depth.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="depth">Number of child levels to expand. 0 = node only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<OrchestrationNodeConfiguration>> GetNodeDeep(
        Guid nodeId,
        int depth,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new orchestration node.
    /// </summary>
    Task<IGenericResult<OrchestrationNodeConfiguration>> CreateNode(
        OrchestrationNodeConfiguration request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing orchestration node. NodeType and parent linkage are immutable.
    /// </summary>
    Task<IGenericResult<OrchestrationNodeConfiguration>> UpdateNode(
        Guid nodeId,
        OrchestrationNodeConfiguration request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an orchestration node by its logical identifier.
    /// </summary>
    Task<IGenericResult> DeleteNode(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}
