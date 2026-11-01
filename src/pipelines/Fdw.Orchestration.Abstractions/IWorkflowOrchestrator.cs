using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Orchestrates workflow execution across multiple pipelines.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the workflow execution result.</returns>
    Task<IGenericResult<IWorkflowExecutionResult>> ExecuteWorkflow(
        IWorkflow workflow,
        IWorkflowExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a running workflow.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the workflow status.</returns>
    Task<IGenericResult<ICurrentWorkflowStatus>> GetStatus(
        string workflowExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a running workflow.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> Pause(
        string workflowExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused workflow.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> Resume(
        string workflowExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> Cancel(
        string workflowExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed workflow step.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="stepId">The step ID to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> RetryStep(
        string workflowExecutionId,
        string stepId,
        CancellationToken cancellationToken = default);
}