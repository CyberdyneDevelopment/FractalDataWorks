using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Execute a custom action.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Custom", RestrictToCurrentCompilation = true)]
public sealed class CustomStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomStepType"/> class.
    /// </summary>
    public CustomStepType() : base(5, "Custom", executesPipeline: false, supportsParallelism: true) { }
}
