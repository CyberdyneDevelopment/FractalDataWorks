using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow completed successfully.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "Succeeded", RestrictToCurrentCompilation = true)]
public sealed class SucceededExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SucceededExecutionStatus"/> class.
    /// </summary>
    public SucceededExecutionStatus() : base(3, "Succeeded", isTerminal: true, isSuccess: true, isFailure: false) { }
}
