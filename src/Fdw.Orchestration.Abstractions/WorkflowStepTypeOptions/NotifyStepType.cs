using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Send a notification.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkflowStepTypes), "Notify", RestrictToCurrentCompilation = true)]
public sealed class NotifyStepType : WorkflowStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyStepType"/> class.
    /// </summary>
    public NotifyStepType() : base(4, "Notify", executesPipeline: false, supportsParallelism: true) { }
}
