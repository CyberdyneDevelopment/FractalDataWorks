using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>
/// Base class for duration unit type options.
/// </summary>
public abstract class DurationUnitTypeBase : TypeOptionBase<DurationUnitTypeBase>, IDurationUnitType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurationUnitTypeBase"/> class.
    /// </summary>
    /// <param name="name">The unit name (e.g., "Hours", "Minutes").</param>
    protected DurationUnitTypeBase(string name)
        : base(name, "DurationUnits")
    {
    }

    /// <inheritdoc/>
    public abstract TimeSpan ToTimeSpan(double amount);
}
