using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions;

/// <summary>
/// Represents a schedule type that defines how a schedule specifies its execution timing.
/// </summary>
public interface IScheduleType : ITypeOption<int, ScheduleTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this schedule type requires a cron expression.
    /// </summary>
    bool RequiresCronExpression { get; }

    /// <summary>
    /// Gets a value indicating whether this schedule type requires an interval duration.
    /// </summary>
    bool RequiresIntervalDuration { get; }

    /// <summary>
    /// Gets a value indicating whether this schedule type requires a specific one-time date/time.
    /// </summary>
    bool RequiresOneTimeDateTime { get; }

    /// <summary>
    /// Gets a value indicating whether this schedule type requires an event name.
    /// </summary>
    bool RequiresEventName { get; }
}
