#pragma warning disable CS1591
using System.Data;

namespace Fdw.Schema.Properties;

/// <summary>
/// Represents a physical database column with SQL type information.
/// Use for database schema operations, DDL generation, and SQL query building.
/// </summary>
public interface IColumnDefinition : IPropertyDefinition
{
    /// <summary>
    /// The SQL data type (e.g., SqlDbType.VarChar, SqlDbType.Int).
    /// </summary>
    SqlDbType SqlType { get; }

    /// <summary>
    /// Maximum length for string/binary types. Null for fixed-size types.
    /// Use -1 for MAX (VARCHAR(MAX), VARBINARY(MAX)).
    /// </summary>
    int? MaxLength { get; }

    /// <summary>
    /// Precision for decimal/numeric types.
    /// </summary>
    int? Precision { get; }

    /// <summary>
    /// Scale for decimal/numeric types.
    /// </summary>
    int? Scale { get; }

    /// <summary>
    /// Whether this column is an identity column (auto-increment).
    /// </summary>
    bool IsIdentity { get; }

    /// <summary>
    /// SQL expression for default value (e.g., "GETDATE()", "1", "'N/A'").
    /// </summary>
    string? DefaultExpression { get; }

    /// <summary>
    /// SQL expression for computed columns.
    /// </summary>
    string? ComputedExpression { get; }

    /// <summary>
    /// Collation for string columns (e.g., "SQL_Latin1_General_CP1_CI_AS").
    /// </summary>
    string? Collation { get; }
}
