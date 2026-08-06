using System;
using System.Data;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for data type converters following ConnectionStates pattern.
/// Converts between source data types (SQL, JSON, etc.) and CLR types.
/// ALWAYS provides DbType metadata for parameter creation.
/// </summary>
/// <remarks>
/// Examples: SqlInt32Converter (SQL int → CLR int), JsonStringConverter (JSON string → CLR string)
/// </remarks>
public interface IDataTypeConverter : ITypeOption<int, DataTypeConverterBase>
{
    /// <summary>
    /// Source type name (e.g., "int" for SQL, "integer" for JSON).
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// Target CLR type.
    /// </summary>
    Type TargetClrType { get; }

    /// <summary>
    /// Generic ADO.NET DbType - ALWAYS provided for parameter creation.
    /// Essential for creating properly-typed database parameters.
    /// </summary>
    DbType DbType { get; }

    /// <summary>
    /// Convert database value to CLR type.
    /// Returns null for DBNull or null input.
    /// </summary>
    /// <param name="dbValue">Database value.</param>
    /// <returns>CLR value.</returns>
    object? ToClr(object? dbValue);

    /// <summary>
    /// Convert CLR value to database value (for parameters).
    /// </summary>
    /// <param name="clrValue">CLR value.</param>
    /// <returns>Database value.</returns>
    object? ToDb(object? clrValue);

    /// <summary>
    /// Size for variable-length types (varchar, varbinary).
    /// -1 = MAX, null = not applicable.
    /// </summary>
    int? Size { get; }

    /// <summary>
    /// Precision for numeric types (decimal, numeric).
    /// </summary>
    byte? Precision { get; }

    /// <summary>
    /// Scale for numeric types (decimal, numeric).
    /// </summary>
    byte? Scale { get; }
}
