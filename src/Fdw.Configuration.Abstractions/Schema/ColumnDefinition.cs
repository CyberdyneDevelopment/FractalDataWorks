using System.Globalization;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Represents a column definition for DDL generation.
/// </summary>
public sealed class ColumnDefinition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the SQL data type (e.g., "nvarchar", "int", "uniqueidentifier").
    /// </summary>
    public string SqlType { get; set; } = "";

    /// <summary>
    /// Gets or sets the max length for string/binary types.
    /// Use -1 for MAX.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the precision for decimal types.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for decimal types.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets whether this is an identity column.
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Gets or sets whether this column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets the default value expression (SQL syntax).
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets the full SQL type string (e.g., "nvarchar(100)", "decimal(18,2)").
    /// </summary>
    public string GetFullSqlType()
    {
        if (MaxLength.HasValue)
        {
            var length = MaxLength.Value == -1 ? "max" : MaxLength.Value.ToString(CultureInfo.InvariantCulture);
            return $"{SqlType}({length})";
        }

        if (Precision.HasValue && Scale.HasValue)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}({1},{2})", SqlType, Precision.Value, Scale.Value);
        }

        if (Precision.HasValue)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}({1})", SqlType, Precision.Value);
        }

        return SqlType;
    }
}
