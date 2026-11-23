using System;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>One kind of value a filter compares as a number, and how it reads as a decimal.</summary>
public interface IFilterNumberKind : ITypeOption<int, FilterNumberKindBase>
{
    /// <summary>Tells whether a value is of this kind, and, for text, a number.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Reads a value of this kind as a decimal.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The decimal.</returns>
    decimal ToDecimal(object value);
}
