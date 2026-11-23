using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>One type of date or time a duration can be added to.</summary>
public interface IDurationTargetKind : ITypeOption<int, DurationTargetKindBase>
{
    /// <summary>Tells whether a value is of this type.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this type holds the value.</returns>
    bool Holds(object value);

    /// <summary>Adds a duration to a value of this type.</summary>
    /// <param name="value">A value this type <see cref="Holds"/>.</param>
    /// <param name="duration">The duration.</param>
    /// <returns>The later value, of the same type.</returns>
    object Add(object value, TimeSpan duration);
}
