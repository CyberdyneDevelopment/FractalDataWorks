using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow failed.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Failed", RestrictToCurrentCompilation = true)]
public sealed class FailedExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedExecutionStatus"/> class.
    /// </summary>
    public FailedExecutionStatus() : base(5, "Failed", isTerminal: true, isSuccess: false, isFailure: true) { }
}
