using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions.Options;

/// <summary>
/// Schedule type for cron-expression-based scheduling.
/// </summary>
/// <remarks>
/// Cron schedule types require a valid cron expression (e.g., "0 9 * * MON-FRI") to determine
/// when execution should occur. Supports complex recurring schedules across minutes, hours, days,
/// months, and weekdays.
/// </remarks>
[TypeOption(typeof(ScheduleTypes), "Cron")]
[ExcludeFromCodeCoverage]
public sealed class CronScheduleType : ScheduleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronScheduleType"/> class.
    /// </summary>
    public CronScheduleType()
        : base(
            id: 1,
            name: "Cron",
            requiresCronExpression: true,
            requiresIntervalDuration: false,
            requiresOneTimeDateTime: false,
            requiresEventName: false)
    {
    }
}
