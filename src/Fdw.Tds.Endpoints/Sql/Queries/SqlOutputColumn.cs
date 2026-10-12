using System;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// One column of a query's result before its type is settled: its name, and the type its source declares
/// when it declares one.
/// </summary>
public sealed class SqlOutputColumn
{
    /// <summary>Initializes a new instance of the <see cref="SqlOutputColumn"/> class.</summary>
    /// <param name="name">The column's name; empty for an expression with no alias.</param>
    /// <param name="declared">The type its source declares, or null when it is known only from its values.</param>
    /// <param name="isNullable">Whether it may hold NULL.</param>
    public SqlOutputColumn(string name, TdsColumnType? declared, bool isNullable)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Declared = declared;
        IsNullable = isNullable;
    }

    /// <summary>Gets the column's name.</summary>
    public string Name { get; }

    /// <summary>Gets the type its source declares, or null.</summary>
    public TdsColumnType? Declared { get; }

    /// <summary>Gets a value indicating whether it may hold NULL.</summary>
    public bool IsNullable { get; }
}
