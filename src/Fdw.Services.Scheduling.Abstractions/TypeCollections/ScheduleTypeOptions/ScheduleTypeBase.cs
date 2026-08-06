using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions;

/// <summary>
/// Base class for schedule types using the CRTP pattern.
/// </summary>
/// <remarks>
/// Schedule types define how a schedule specifies its execution timing.
/// Each derived type enables one specific scheduling mode via a dedicated boolean flag,
/// while all other flags default to <c>false</c>.
/// </remarks>
public abstract class ScheduleTypeBase : TypeOptionBase<int, ScheduleTypeBase>, IScheduleType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="requiresCronExpression">Whether this schedule type requires a cron expression.</param>
    /// <param name="requiresIntervalDuration">Whether this schedule type requires an interval duration.</param>
    /// <param name="requiresOneTimeDateTime">Whether this schedule type requires a specific one-time date/time.</param>
    /// <param name="requiresEventName">Whether this schedule type requires an event name.</param>
    protected ScheduleTypeBase(
        int id,
        string name,
        bool requiresCronExpression,
        bool requiresIntervalDuration,
        bool requiresOneTimeDateTime,
        bool requiresEventName)
        : base(id, name)
    {
        RequiresCronExpression = requiresCronExpression;
        RequiresIntervalDuration = requiresIntervalDuration;
        RequiresOneTimeDateTime = requiresOneTimeDateTime;
        RequiresEventName = requiresEventName;
    }

    /// <inheritdoc/>
    public bool RequiresCronExpression { get; }

    /// <inheritdoc/>
    public bool RequiresIntervalDuration { get; }

    /// <inheritdoc/>
    public bool RequiresOneTimeDateTime { get; }

    /// <inheritdoc/>
    public bool RequiresEventName { get; }
}
