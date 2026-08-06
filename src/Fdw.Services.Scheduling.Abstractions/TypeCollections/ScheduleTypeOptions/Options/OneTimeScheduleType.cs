using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions.Options;

/// <summary>
/// Schedule type for one-time execution at a specific date and time.
/// </summary>
/// <remarks>
/// One-time schedule types require a specific date and time at which execution will occur exactly once.
/// After the scheduled execution completes, the schedule is considered fulfilled and will not repeat.
/// </remarks>
[TypeOption(typeof(ScheduleTypes), "OneTime")]
[ExcludeFromCodeCoverage]
public sealed class OneTimeScheduleType : ScheduleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneTimeScheduleType"/> class.
    /// </summary>
    public OneTimeScheduleType()
        : base(
            id: 3,
            name: "OneTime",
            requiresCronExpression: false,
            requiresIntervalDuration: false,
            requiresOneTimeDateTime: true,
            requiresEventName: false)
    {
    }
}
