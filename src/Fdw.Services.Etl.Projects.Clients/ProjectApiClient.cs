using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// HTTP API client for ETL project orchestration endpoints.
/// Un-sealed with virtual methods for Moq testability per FDW test conventions.
/// </summary>
public class ProjectApiClient : ApiClientBase, IProjectApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectApiClient"/> class.
    /// </summary>
    public ProjectApiClient(HttpClient httpClient, ILogger<ProjectApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    public virtual Task<IGenericResult<IReadOnlyList<ProjectConfiguration>>> ListProjects(
        CancellationToken cancellationToken = default)
        => Get<IReadOnlyList<ProjectConfiguration>>("projects", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<ProjectConfiguration>> GetProject(
        Guid projectId,
        CancellationToken cancellationToken = default)
        => Get<ProjectConfiguration>($"projects/{projectId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<ProjectConfiguration>> CreateProject(
        ProjectConfiguration request,
        CancellationToken cancellationToken = default)
        => Post<ProjectConfiguration, ProjectConfiguration>("projects", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<ProjectConfiguration>> UpdateProject(
        Guid projectId,
        ProjectConfiguration request,
        CancellationToken cancellationToken = default)
        => Put<ProjectConfiguration, ProjectConfiguration>($"projects/{projectId}", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult> DeleteProject(
        Guid projectId,
        CancellationToken cancellationToken = default)
        => Delete($"projects/{projectId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<TriggerResponse>> Trigger(
        string type,
        TriggerRequest request,
        CancellationToken cancellationToken = default)
        => Post<TriggerRequest, TriggerResponse>($"etl/trigger/{type}", request, cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult<ProjectExecutionStatusNode>> GetExecutionStatus(
        Guid executionItemId,
        CancellationToken cancellationToken = default)
        => Get<ProjectExecutionStatusNode>($"etl/executions/{executionItemId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult> CancelExecution(
        Guid executionItemId,
        CancellationToken cancellationToken = default)
        => Delete($"etl/executions/{executionItemId}", cancellationToken);

    /// <inheritdoc/>
    public virtual Task<IGenericResult> ApproveExecution(
        Guid executionItemId,
        CancellationToken cancellationToken = default)
        => Post($"etl/executions/{executionItemId}/approve", cancellationToken);
}
