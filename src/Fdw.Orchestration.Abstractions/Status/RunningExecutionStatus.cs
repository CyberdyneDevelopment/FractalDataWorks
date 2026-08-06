using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow is running.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Running", RestrictToCurrentCompilation = true)]
public sealed class RunningExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunningExecutionStatus"/> class.
    /// </summary>
    public RunningExecutionStatus() : base(1, "Running", isTerminal: false, isSuccess: false, isFailure: false) { }
}
