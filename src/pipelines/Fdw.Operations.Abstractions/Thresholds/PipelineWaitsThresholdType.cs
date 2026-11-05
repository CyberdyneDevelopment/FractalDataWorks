using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Waiting runs as a threshold (FDW-3903): the runs of one pipeline that are waiting to start, judged each time the queue changes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "PipelineWaits")]
public sealed class PipelineWaitsThresholdType : ThresholdTypeBase
{
    /// <summary>The name of the field this type reports.</summary>
    public const string WaitingRuns = "waitingRuns";

    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField(WaitingRuns, typeof(long), "runs", "How many runs are waiting to start: the runs of one pipeline that are waiting to start."),
    ];

    /// <summary>Initializes a new instance of the <see cref="PipelineWaitsThresholdType"/> class.</summary>
    public PipelineWaitsThresholdType()
        : base("PipelineWaits", "How many runs are waiting to start: the runs of one pipeline that are waiting to start.")
    {
    }

    /// <inheritdoc/>
    public override string SubjectKind => "Pipeline";

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;
}
