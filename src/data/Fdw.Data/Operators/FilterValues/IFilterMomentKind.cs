using System;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>One kind of value a filter compares as a point in time, and how it reads as one.</summary>
public interface IFilterMomentKind : ITypeOption<int, FilterMomentKindBase>
{
    /// <summary>Tells whether a value is of this kind, and, for text, a date and time.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Reads a value of this kind as a date and time with an offset.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The date and time.</returns>
    DateTimeOffset ToMoment(object value);
}
