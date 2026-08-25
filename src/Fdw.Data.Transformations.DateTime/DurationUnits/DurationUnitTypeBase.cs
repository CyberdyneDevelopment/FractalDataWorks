using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>
/// Base class for duration unit type options.
/// </summary>
public abstract class DurationUnitTypeBase : TypeOptionBase<int, DurationUnitTypeBase>, IDurationUnitType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurationUnitTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unit name (e.g., "Hours", "Minutes").</param>
    protected DurationUnitTypeBase(int id, string name)
        : base(id, name, "DurationUnits")
    {
    }

    /// <inheritdoc/>
    public abstract TimeSpan ToTimeSpan(double amount);
}
