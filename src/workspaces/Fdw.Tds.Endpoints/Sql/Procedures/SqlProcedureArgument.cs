using System;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// One argument a procedure is called with: by position or by name, and whether it is an OUTPUT.
/// </summary>
public sealed class SqlProcedureArgument
{
    /// <summary>Initializes a new instance of the <see cref="SqlProcedureArgument"/> class.</summary>
    /// <param name="ordinal">Its position in the call, from 0.</param>
    /// <param name="name">Its name with the leading @, or empty when passed by position.</param>
    /// <param name="value">Its value; null for NULL.</param>
    /// <param name="isOutput">Whether the caller passed it to receive a value back.</param>
    public SqlProcedureArgument(int ordinal, string name, object? value, bool isOutput)
    {
        Ordinal = ordinal;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        IsOutput = isOutput;
    }

    /// <summary>Gets its position in the call, from 0.</summary>
    public int Ordinal { get; }

    /// <summary>Gets its name with the leading @, or empty when passed by position.</summary>
    public string Name { get; }

    /// <summary>Gets its value; null for NULL.</summary>
    public object? Value { get; }

    /// <summary>Gets a value indicating whether the caller passed it to receive a value back.</summary>
    public bool IsOutput { get; }
}
