using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Abstractions.Distribution;

/// <summary>
/// Interface for distributed workflow execution.
/// Implementations can route workflows to different workers/nodes.
/// Default implementation executes locally; custom implementations
/// can use message queues, task schedulers, or orchestration platforms.
/// </summary>
public interface IWorkflowDistributor
{
    /// <summary>
    /// Gets whether this distributor supports distributed execution.
    /// </summary>
    bool IsDistributed { get; }

    /// <summary>
    /// Gets the distributor name.
    /// </summary>
    string DistributorName { get; }

    /// <summary>
    /// Distributes a workflow step for execution.
    /// </summary>
    /// <param name="workflow">The workflow being executed.</param>
    /// <param name="step">The step to execute.</param>
    /// <param name="context">The workflow execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the step execution result.</returns>
    Task<IGenericResult<IWorkflowStepResult>> ExecuteStep(
        IWorkflow workflow,
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a workflow can be distributed.
    /// </summary>
    /// <param name="workflow">The workflow to check.</param>
    /// <returns>True if the workflow can be distributed.</returns>
    bool CanDistribute(IWorkflow workflow);

    /// <summary>
    /// Gets the status of a distributed step execution.
    /// </summary>
    /// <param name="stepExecutionId">The step execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the step status.</returns>
    Task<IGenericResult<IWorkflowStepResult>> GetStepStatus(
        string stepExecutionId,
        CancellationToken cancellationToken = default);
}
