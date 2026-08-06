using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// Workflow completed with warnings.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowExecutionStatuses), "SucceededWithWarnings", RestrictToCurrentCompilation = true)]
public sealed class SucceededWithWarningsExecutionStatus : WorkflowExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SucceededWithWarningsExecutionStatus"/> class.
    /// </summary>
    public SucceededWithWarningsExecutionStatus() : base(4, "SucceededWithWarnings", isTerminal: true, isSuccess: true, isFailure: false) { }
}
