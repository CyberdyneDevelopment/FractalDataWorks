using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A pipeline as the subject of a threshold; its link table is pipe.PipelineThreshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "Pipeline")]
public sealed class PipelineThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineThresholdSubjectKind"/> class.</summary>
    public PipelineThresholdSubjectKind()
        : base("Pipeline", "A pipeline", "pipe", "PipelineThreshold")
    {
    }

    /// <inheritdoc />
    public override Citation Cites => Citation.EnforcedByDatabase("FK_PipelineThreshold_Pipeline");
}
