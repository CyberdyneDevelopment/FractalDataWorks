using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A component of a pipeline graph as the subject of a threshold; its link table is pipe.PipelineComponentThreshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "Component")]
public sealed class ComponentThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ComponentThresholdSubjectKind"/> class.</summary>
    public ComponentThresholdSubjectKind()
        : base("Component", "A component of a pipeline graph", "pipe", "PipelineComponentThreshold")
    {
    }

    /// <inheritdoc />
    public override Citation Cites => Citation.Uncited("a component is a node of a pipeline graph, not a row of a domain a check can ask");
}
