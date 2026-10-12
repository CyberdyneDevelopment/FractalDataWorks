using System;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// A value a procedure hands back through an OUTPUT argument.
/// </summary>
public sealed class SqlProcedureOutput
{
    /// <summary>Initializes a new instance of the <see cref="SqlProcedureOutput"/> class.</summary>
    /// <param name="ordinal">The argument's position in the call.</param>
    /// <param name="name">The argument's name, with its leading @, or empty.</param>
    /// <param name="value">The value; null for NULL.</param>
    /// <param name="type">The SQL type the value is written as.</param>
    public SqlProcedureOutput(int ordinal, string name, object? value, TdsColumnType type)
    {
        Ordinal = ordinal;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Gets the argument's position in the call.</summary>
    public int Ordinal { get; }

    /// <summary>Gets the argument's name.</summary>
    public string Name { get; }

    /// <summary>Gets the value.</summary>
    public object? Value { get; }

    /// <summary>Gets the SQL type the value is written as.</summary>
    public TdsColumnType Type { get; }
}
