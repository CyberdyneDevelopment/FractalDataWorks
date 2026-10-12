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
    public virtual Task<IGenericResult<IReadOnlyList<OrchestrationNodeImplementationConfiguration>>> ListRootNodes(
        CancellationToken cancellationToken = default)
        => Get<IReadOnlyList<OrchestrationNodeImplementationConfiguration>>("nodes", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> GetNode(
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Get<OrchestrationNodeImplementationConfiguration>($"nodes/{nodeId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> GetNodeDeep(
        Guid nodeId,
        int depth,
        CancellationToken cancellationToken = default)
        => Get<OrchestrationNodeImplementationConfiguration>($"nodes/{nodeId}?depth={depth}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> CreateNode(
        OrchestrationNodeImplementationConfiguration request,
        CancellationToken cancellationToken = default)
        => Post<OrchestrationNodeImplementationConfiguration, OrchestrationNodeImplementationConfiguration>("nodes", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> UpdateNode(
        Guid nodeId,
        OrchestrationNodeImplementationConfiguration request,
        CancellationToken cancellationToken = default)
        => Patch<OrchestrationNodeImplementationConfiguration, OrchestrationNodeImplementationConfiguration>($"nodes/{nodeId}", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult> DeleteNode(
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Delete($"nodes/{nodeId}", cancellationToken);
}
