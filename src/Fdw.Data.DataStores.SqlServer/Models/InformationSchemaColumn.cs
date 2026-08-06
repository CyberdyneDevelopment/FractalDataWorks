namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// Represents a row from INFORMATION_SCHEMA.COLUMNS with additional metadata.
/// </summary>
public sealed class InformationSchemaColumn
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type (e.g., "nvarchar", "int").
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is nullable ("YES" or "NO").
    /// </summary>
    public string IsNullable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum character length.
    /// </summary>
    public int? CharacterMaximumLength { get; set; }

    /// <summary>
    /// Gets or sets the numeric precision.
    /// </summary>
    public byte? NumericPrecision { get; set; }

    /// <summary>
    /// Gets or sets the numeric scale.
    /// </summary>
    public int? NumericScale { get; set; }

    /// <summary>
    /// Gets or sets the column default value.
    /// </summary>
    public string? ColumnDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this column is a primary key (1 or 0).
    /// </summary>
    public int IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets whether this column is an identity column (1 or 0).
    /// </summary>
    public int IsIdentity { get; set; }
}
