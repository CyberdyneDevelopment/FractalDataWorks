using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// HTTP API client for generic orchestration node endpoints.
/// Un-sealed with virtual methods for Moq testability per FDW test conventions.
/// </summary>
public class NodeApiClient : ApiClientBase, INodeApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeApiClient"/> class.
    /// </summary>
    public NodeApiClient(HttpClient httpClient, ILogger<NodeApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    public virtual Task<IGenericResult<IReadOnlyList<OrchestrationNodeConfiguration>>> ListRootNodes(
        CancellationToken cancellationToken = default)
        => Get<IReadOnlyList<OrchestrationNodeConfiguration>>("nodes", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeConfiguration>> GetNode(
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Get<OrchestrationNodeConfiguration>($"nodes/{nodeId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeConfiguration>> GetNodeDeep(
        Guid nodeId,
        int depth,
        CancellationToken cancellationToken = default)
        => Get<OrchestrationNodeConfiguration>($"nodes/{nodeId}?depth={depth}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeConfiguration>> CreateNode(
        OrchestrationNodeConfiguration request,
        CancellationToken cancellationToken = default)
        => Post<OrchestrationNodeConfiguration, OrchestrationNodeConfiguration>("nodes", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeConfiguration>> UpdateNode(
        Guid nodeId,
        OrchestrationNodeConfiguration request,
        CancellationToken cancellationToken = default)
        => Patch<OrchestrationNodeConfiguration, OrchestrationNodeConfiguration>($"nodes/{nodeId}", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult> DeleteNode(
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Delete($"nodes/{nodeId}", cancellationToken);
}
