using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>One type a numeric transformer accepts as input, and how it widens to a decimal.</summary>
public interface INumericInputKind : ITypeOption<int, NumericInputKindBase>
{
    /// <summary>Tells whether an input is of this type.</summary>
    /// <param name="value">The input, which is not null.</param>
    /// <returns>True when this type holds the input.</returns>
    bool Holds(object value);

    /// <summary>Widens an input of this type to a decimal.</summary>
    /// <param name="value">An input this type <see cref="Holds"/>.</param>
    /// <returns>The decimal.</returns>
    decimal ToDecimal(object value);
}
