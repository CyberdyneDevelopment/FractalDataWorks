#pragma warning disable CS1591
using System;
using System.Text;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>
/// Defines a database column for DDL generation.
/// </summary>
public sealed class DdlColumnDefinition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the SQL data type (e.g., "VARCHAR", "INT", "DECIMAL").
    /// </summary>
    public required string SqlType { get; init; }

    /// <summary>
    /// Gets or sets the maximum length for string/binary types.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets or sets the precision for numeric types.
    /// </summary>
    public int? Precision { get; init; }

    /// <summary>
    /// Gets or sets the scale for decimal types.
    /// </summary>
    public int? Scale { get; init; }

    /// <summary>
    /// Gets or sets whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; init; } = true;

    /// <summary>
    /// Gets or sets whether this column is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Gets or sets whether this column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets or sets the default value expression.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets or sets the collation for string columns.
    /// </summary>
    public string? Collation { get; init; }

    /// <summary>
    /// Gets or sets the computed column expression.
    /// </summary>
    public string? ComputedExpression { get; init; }

    /// <summary>
    /// Gets the full SQL type with length/precision/scale.
    /// </summary>
    /// <returns>The formatted SQL type string (e.g., "VARCHAR(255)", "DECIMAL(18,2)").</returns>
    public string GetFullSqlType()
    {
        var sb = new StringBuilder(SqlType);

        if (MaxLength.HasValue)
        {
            sb.Append('(');
            sb.Append(MaxLength.Value == -1 ? "MAX" : MaxLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(')');
        }
        else if (Precision.HasValue)
        {
            sb.Append('(');
            sb.Append(Precision.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (Scale.HasValue)
            {
                sb.Append(',');
                sb.Append(Scale.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append(')');
        }

        return sb.ToString();
    }
}
