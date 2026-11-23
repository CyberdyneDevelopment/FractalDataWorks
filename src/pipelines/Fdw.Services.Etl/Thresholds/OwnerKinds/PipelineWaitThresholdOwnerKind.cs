using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A pipeline: it has to exist.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WaitThresholdOwnerKinds), "Pipeline")]
public sealed class PipelineWaitThresholdOwnerKind : WaitThresholdOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineWaitThresholdOwnerKind"/> class.</summary>
    public PipelineWaitThresholdOwnerKind()
        : base(WaitThresholdOwners.Pipeline)
    {
    }

    /// <inheritdoc />
    public override bool OwnerExists(string owner, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules) => pipelines.ContainsKey(owner);
}
