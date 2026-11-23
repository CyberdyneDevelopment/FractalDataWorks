using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>One type a date-time transformer accepts for a whole-number input, and how it reads as a long.</summary>
public interface IWholeNumberInputKind : ITypeOption<int, WholeNumberInputKindBase>
{
    /// <summary>Tells whether an input is of this type, and, for text, a whole number.</summary>
    /// <param name="value">The input, which is not null.</param>
    /// <returns>True when this type holds the input.</returns>
    bool Holds(object value);

    /// <summary>Reads an input of this type as a long.</summary>
    /// <param name="value">An input this type <see cref="Holds"/>.</param>
    /// <returns>The whole number.</returns>
    long Read(object value);
}
