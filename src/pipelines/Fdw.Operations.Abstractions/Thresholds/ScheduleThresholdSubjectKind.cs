using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A schedule as the subject of a threshold; its link table is sched.ScheduleThreshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "Schedule")]
public sealed class ScheduleThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleThresholdSubjectKind"/> class.</summary>
    public ScheduleThresholdSubjectKind()
        : base("Schedule", "A schedule", "sched", "ScheduleThreshold")
    {
    }
}
