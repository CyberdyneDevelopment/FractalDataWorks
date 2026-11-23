using System;
using Fdw.Collections;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>One shape a row can have, and how a column is read from a row of that shape.</summary>
public interface IRowKind : ITypeOption<int, RowKindBase>
{
    /// <summary>Tells whether a row has this shape.</summary>
    /// <param name="row">The row, which is not null.</param>
    /// <returns>True when this kind holds the row.</returns>
    bool Holds(object row);

    /// <summary>Reads a column of a row of this shape.</summary>
    /// <param name="row">A row this kind <see cref="Holds"/>.</param>
    /// <param name="column">The column name: found by its exact name first, then regardless of case.</param>
    /// <returns>What the read found.</returns>
    RowReading Read(object row, string column);

    /// <summary>Gives the type a SQL parameter binds the column as.</summary>
    /// <param name="reading">What <see cref="Read"/> found.</param>
    /// <param name="columnType">The column's CLR type, which a null read from a dictionary row binds as.</param>
    /// <returns>The type to bind as; null when the column is absent or the value carries its own type.</returns>
    Type? BindingType(RowReading reading, Type columnType);
}
