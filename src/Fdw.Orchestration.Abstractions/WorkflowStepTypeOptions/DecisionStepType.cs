using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Execute a decision/branch.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Decision", RestrictToCurrentCompilation = true)]
public sealed class DecisionStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionStepType"/> class.
    /// </summary>
    public DecisionStepType() : base(1, "Decision", executesPipeline: false, supportsParallelism: false) { }
}
