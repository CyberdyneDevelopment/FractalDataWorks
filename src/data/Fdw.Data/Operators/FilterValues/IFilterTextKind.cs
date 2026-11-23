using System;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>One kind of value a filter compares as text, and how it is written as text.</summary>
public interface IFilterTextKind : ITypeOption<int, FilterTextKindBase>
{
    /// <summary>Tells whether a value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Writes a value of this kind as text.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The text, in the invariant culture.</returns>
    string Text(object value);
}
