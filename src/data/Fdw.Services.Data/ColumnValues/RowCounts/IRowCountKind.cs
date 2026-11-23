using System;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>One kind of value a store answers a row count as.</summary>
public interface IRowCountKind : ITypeOption<int, RowCountKindBase>
{
    /// <summary>Tells whether a stored value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value, and, for text, reads as one.</returns>
    bool Holds(object value);

    /// <summary>Reads a value of this kind.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The value, or null when it is of this kind but stands for none.</returns>
    long? Read(object value);
}
