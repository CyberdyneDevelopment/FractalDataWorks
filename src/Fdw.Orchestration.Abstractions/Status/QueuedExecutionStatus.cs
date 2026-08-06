using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow is queued.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Queued", RestrictToCurrentCompilation = true)]
public sealed class QueuedExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueuedExecutionStatus"/> class.
    /// </summary>
    public QueuedExecutionStatus() : base(0, "Queued", isTerminal: false, isSuccess: false, isFailure: false) { }
}
