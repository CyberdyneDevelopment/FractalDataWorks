using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Execute a parallel branch.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Parallel", RestrictToCurrentCompilation = true)]
public sealed class ParallelStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStepType"/> class.
    /// </summary>
    public ParallelStepType() : base(2, "Parallel", executesPipeline: false, supportsParallelism: true) { }
}
