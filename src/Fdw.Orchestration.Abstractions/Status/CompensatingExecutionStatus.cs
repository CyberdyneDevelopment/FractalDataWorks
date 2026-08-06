using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow is being compensated.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Compensating", RestrictToCurrentCompilation = true)]
public sealed class CompensatingExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompensatingExecutionStatus"/> class.
    /// </summary>
    public CompensatingExecutionStatus() : base(8, "Compensating", isTerminal: false, isSuccess: false, isFailure: false) { }
}
