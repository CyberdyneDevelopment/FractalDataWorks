using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Execute a pipeline.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Pipeline", RestrictToCurrentCompilation = true)]
public sealed class PipelineStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStepType"/> class.
    /// </summary>
    public PipelineStepType() : base(0, "Pipeline", executesPipeline: true, supportsParallelism: true) { }
}
