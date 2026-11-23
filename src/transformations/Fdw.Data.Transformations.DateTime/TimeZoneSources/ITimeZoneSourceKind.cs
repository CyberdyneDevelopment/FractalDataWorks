using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>One type of value a time zone can be applied to.</summary>
public interface ITimeZoneSourceKind : ITypeOption<int, TimeZoneSourceKindBase>
{
    /// <summary>Tells whether a value is of this type, and, for text, a date and time.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this type holds the value.</returns>
    bool Holds(object value);

    /// <summary>Expresses a value of this type as a date and time in a time zone.</summary>
    /// <param name="value">A value this type <see cref="Holds"/>.</param>
    /// <param name="timeZone">The time zone.</param>
    /// <returns>The date and time in the zone.</returns>
    DateTimeOffset InZone(object value, TimeZoneInfo timeZone);
}
