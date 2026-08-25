using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>
/// Base class for named timezone type options.
/// </summary>
public abstract class TimeZoneTypeBase : TypeOptionBase<int, TimeZoneTypeBase>, ITimeZoneType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeZoneTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The timezone name (e.g., "UTC", "Central").</param>
    /// <param name="timeZoneId">The system timezone identifier.</param>
    protected TimeZoneTypeBase(int id, string name, string timeZoneId)
        : base(id, name, "TimeZones")
    {
        TimeZoneId = timeZoneId;
    }

    /// <inheritdoc/>
    public string TimeZoneId { get; }

    /// <summary>
    /// Resolves the <see cref="TimeZoneInfo"/> for this timezone.
    /// </summary>
    /// <returns>The resolved timezone info.</returns>
    public TimeZoneInfo Resolve() => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}
