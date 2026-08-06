using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Represents a SQL column definition for DDL generation.
/// </summary>
/// <remarks>Excluded from coverage: pure data class with no logic.</remarks>
[ExcludeFromCodeCoverage]
public sealed class SqlColumnDefinition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = "";

    /// <summary>
    /// Gets or sets the SQL data type (e.g., "varchar", "int", "uniqueidentifier").
    /// </summary>
    public string SqlType { get; set; } = "";

    /// <summary>
    /// Gets or sets the max length for string/binary types.
    /// Use -1 for MAX.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the precision for decimal/numeric types.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for decimal/numeric types.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets whether this column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets the default value expression (SQL syntax).
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this is an identity column.
    /// </summary>
    public bool IsIdentity { get; set; }
}
