using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Handles compensation logic when workflows fail.
/// </summary>
public interface ICompensationHandler
{
    /// <summary>
    /// Executes compensation for a failed workflow.
    /// </summary>
    /// <param name="workflowExecutionId">The workflow execution ID.</param>
    /// <param name="completedSteps">Steps that completed successfully.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the compensation result.</returns>
    Task<IGenericResult<ICompensationResult>> Compensate(
        string workflowExecutionId,
        IReadOnlyList<IWorkflowStepResult> completedSteps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a compensation action for a step.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="compensationAction">The compensation action to execute.</param>
    void RegisterCompensation(string stepId, Func<IWorkflowExecutionContext, CancellationToken, Task<IGenericResult>> compensationAction);

    /// <summary>
    /// Checks if compensation is available for a step.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <returns>True if compensation is available, false otherwise.</returns>
    bool HasCompensation(string stepId);
}

