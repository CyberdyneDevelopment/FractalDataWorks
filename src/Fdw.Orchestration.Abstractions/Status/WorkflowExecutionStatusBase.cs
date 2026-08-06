using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Base class for workflow execution status values.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class WorkflowExecutionStatusBase : TypeOptionBase<int, WorkflowExecutionStatusBase>, IWorkflowExecutionStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionStatusBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this workflow execution status.</param>
    /// <param name="name">The name of this workflow execution status.</param>
    /// <param name="isTerminal">Whether this is a terminal status.</param>
    /// <param name="isSuccess">Whether this represents a successful completion.</param>
    /// <param name="isFailure">Whether this represents a failure.</param>
    protected WorkflowExecutionStatusBase(int id, string name, bool isTerminal, bool isSuccess, bool isFailure)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        IsSuccess = isSuccess;
        IsFailure = isFailure;
    }

    /// <inheritdoc />
    public bool IsTerminal { get; }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure { get; }
}
