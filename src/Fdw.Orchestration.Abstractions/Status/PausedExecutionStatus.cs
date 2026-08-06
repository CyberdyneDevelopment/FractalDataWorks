using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow is paused.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Paused", RestrictToCurrentCompilation = true)]
public sealed class PausedExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PausedExecutionStatus"/> class.
    /// </summary>
    public PausedExecutionStatus() : base(2, "Paused", isTerminal: false, isSuccess: false, isFailure: false) { }
}
