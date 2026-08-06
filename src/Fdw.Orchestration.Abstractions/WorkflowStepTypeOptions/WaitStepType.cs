using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Wait for a condition.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Wait", RestrictToCurrentCompilation = true)]
public sealed class WaitStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WaitStepType"/> class.
    /// </summary>
    public WaitStepType() : base(3, "Wait", executesPipeline: false, supportsParallelism: false) { }
}
