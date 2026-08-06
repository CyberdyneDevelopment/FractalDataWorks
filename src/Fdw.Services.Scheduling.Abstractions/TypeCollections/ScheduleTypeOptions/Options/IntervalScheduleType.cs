using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions.Options;

/// <summary>
/// Schedule type for interval-based scheduling.
/// </summary>
/// <remarks>
/// Interval schedule types require an interval duration that defines the time between executions.
/// Execution repeats at regular intervals (e.g., every 30 minutes, every 2 hours) starting from
/// either the first run or a configured start time.
/// </remarks>
[TypeOption(typeof(ScheduleTypes), "Interval")]
[ExcludeFromCodeCoverage]
public sealed class IntervalScheduleType : ScheduleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalScheduleType"/> class.
    /// </summary>
    public IntervalScheduleType()
        : base(
            id: 2,
            name: "Interval",
            requiresCronExpression: false,
            requiresIntervalDuration: true,
            requiresOneTimeDateTime: false,
            requiresEventName: false)
    {
    }
}
