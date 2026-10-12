using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// Defines the API client contract for ETL project orchestration endpoints.
/// Un-sealed with virtual methods for Moq testability per FDW test conventions.
/// </summary>
public interface IProjectApiClient
{
    /// <summary>
    /// Lists all projects visible to the current tenant.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<ProjectConfiguration>>> ListProjects(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single project by its logical identifier.
    /// </summary>
    Task<IGenericResult<ProjectConfiguration>> GetProject(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new project.
    /// </summary>
    Task<IGenericResult<ProjectConfiguration>> CreateProject(
        ProjectConfiguration request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing project. Applies policy elevation validation at write time.
    /// </summary>
    Task<IGenericResult<ProjectConfiguration>> UpdateProject(
        Guid projectId,
        ProjectConfiguration request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project by its logical identifier.
    /// </summary>
    Task<IGenericResult> DeleteProject(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers execution of the specified project (or pipeline/stage/step) via the unified trigger endpoint.
    /// </summary>
    /// <param name="type">One of: "pipeline", "project", "stage", "step".</param>
    /// <param name="request">Trigger parameters — id or (name + parentPath) to resolve the target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<TriggerResponse>> Trigger(
        string type,
        TriggerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rollup hierarchical execution status for a project execution.
    /// </summary>
    Task<IGenericResult<ProjectExecutionStatusNode>> GetExecutionStatus(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an in-progress project execution.
    /// </summary>
    Task<IGenericResult> CancelExecution(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a project execution that is waiting for manual approval (RequireApprovalToRun).
    /// Transitions the execution from AwaitingApproval to Triggered and enqueues it.
    /// </summary>
    Task<IGenericResult> ApproveExecution(
        Guid executionItemId,
        CancellationToken cancellationToken = default);
}
