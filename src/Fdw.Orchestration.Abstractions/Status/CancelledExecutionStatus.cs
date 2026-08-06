using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow was cancelled.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Cancelled", RestrictToCurrentCompilation = true)]
public sealed class CancelledExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelledExecutionStatus"/> class.
    /// </summary>
    public CancelledExecutionStatus() : base(6, "Cancelled", isTerminal: true, isSuccess: false, isFailure: false) { }
}
