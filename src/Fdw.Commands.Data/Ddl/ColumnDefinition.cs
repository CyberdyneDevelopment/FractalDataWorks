using System.Data;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Metadata for a database column definition.
/// </summary>
/// <remarks>
/// <para>
/// Describes the structure of a column in a CREATE TABLE or ALTER TABLE command.
/// Translators convert this to backend-specific DDL syntax.
/// </para>
/// </remarks>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ColumnDefinition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    /// <value>The name of the column.</value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the SQL data type.
    /// </summary>
    /// <value>The SQL data type (Int, NVarChar, DateTime2, etc.).</value>
    public required SqlDbType Type { get; init; }

    /// <summary>
    /// Gets or sets the maximum length for string/binary types.
    /// </summary>
    /// <value>The maximum length, or null if not applicable. Use -1 for MAX.</value>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets or sets the precision for numeric types.
    /// </summary>
    /// <value>The total number of digits, or null if not applicable.</value>
    public int? Precision { get; init; }

    /// <summary>
    /// Gets or sets the scale for numeric types.
    /// </summary>
    /// <value>The number of digits after the decimal point, or null if not applicable.</value>
    public int? Scale { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is required (NOT NULL).
    /// </summary>
    /// <value>True if the column cannot contain NULL values; otherwise, false.</value>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is an identity column (auto-increment).
    /// </summary>
    /// <value>True if the column is an identity column; otherwise, false.</value>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Gets or sets the default value expression for the column.
    /// </summary>
    /// <value>The default value SQL expression (e.g., "GETUTCDATE()", "0", "'Default'"), or null if no default.</value>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is computed.
    /// </summary>
    /// <value>True if the column is computed; otherwise, false.</value>
    public bool IsComputed { get; init; }

    /// <summary>
    /// Gets or sets the computed expression for computed columns.
    /// </summary>
    /// <value>The SQL expression for computed columns, or null if not computed.</value>
    public string? ComputedExpression { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is unique.
    /// </summary>
    /// <value>True if the column has a unique constraint; otherwise, false.</value>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets or sets the collation for string columns.
    /// </summary>
    /// <value>The collation name (e.g., "Latin1_General_CI_AS"), or null for default.</value>
    public string? Collation { get; init; }
}
