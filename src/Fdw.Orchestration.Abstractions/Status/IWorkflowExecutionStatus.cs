using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Interface for workflow execution status values.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IWorkflowExecutionStatus : ITypeOption<int, WorkflowExecutionStatusBase>
{
    /// <summary>
    /// Gets a value indicating whether this is a terminal status (workflow is complete).
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether this represents a successful completion.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether this represents a failure.
    /// </summary>
    bool IsFailure { get; }
}
