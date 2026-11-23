using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Failed fires of every schedule of a tenant as a threshold (FDW-6144): the same count as <see cref="ScheduleFailsThresholdType"/>, one threshold for all the tenant's schedules.</summary>
/// <remarks>
/// It is attached to the tenant's Server subject, and each schedule is judged against it as itself: the schedule's own subject is what a rule of it fires on, so one schedule's count never stands in for another's.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "ServerScheduleFails")]
public sealed class ServerScheduleFailsThresholdType : ThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField(ScheduleFailsThresholdType.ConsecutiveFailures, typeof(long), "fires", "How many fires of a schedule of the tenant in a row failed to start a run."),
    ];

    /// <summary>Initializes a new instance of the <see cref="ServerScheduleFailsThresholdType"/> class.</summary>
    public ServerScheduleFailsThresholdType()
        : base("ServerScheduleFails", "How many fires of a schedule of the tenant in a row failed to start a run.")
    {
    }

    /// <inheritdoc/>
    public override string SubjectKind => "Server";

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;
}
