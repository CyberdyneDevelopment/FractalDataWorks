using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow timed out.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "TimedOut", RestrictToCurrentCompilation = true)]
public sealed class TimedOutExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimedOutExecutionStatus"/> class.
    /// </summary>
    public TimedOutExecutionStatus() : base(7, "TimedOut", isTerminal: true, isSuccess: false, isFailure: true) { }
}
