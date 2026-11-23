using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Failed fires of a schedule as a threshold (FDW-6144): how many fires of one schedule in a row failed to start a run, judged each time a fire's outcome is recorded.</summary>
/// <remarks>
/// A fire that fails is one the scheduler recorded as failed: the ETL host refused it (its grant has ended, its person may no longer act) or could not be reached. A fire that succeeds starts the count again.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "ScheduleFails")]
public sealed class ScheduleFailsThresholdType : ThresholdTypeBase
{
    /// <summary>The name of the field this type and <see cref="ServerScheduleFailsThresholdType"/> report.</summary>
    public const string ConsecutiveFailures = "consecutiveFailures";

    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField(ConsecutiveFailures, typeof(long), "fires", "How many fires of the schedule in a row failed to start a run."),
    ];

    /// <summary>Initializes a new instance of the <see cref="ScheduleFailsThresholdType"/> class.</summary>
    public ScheduleFailsThresholdType()
        : base("ScheduleFails", "How many fires of one schedule in a row failed to start a run.")
    {
    }

    /// <inheritdoc/>
    public override string SubjectKind => "Schedule";

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;
}
