using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// Represents a row from INFORMATION_SCHEMA.COLUMNS with additional metadata.
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaColumn
{
    /// <summary>
    /// Gets or sets the schema of the table or view the column belongs to.
    /// </summary>
    [Column("TABLE_SCHEMA")]
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the table or view the column belongs to.
    /// </summary>
    [Column("TABLE_NAME")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column's position in its table or view.
    /// </summary>
    [Column("ORDINAL_POSITION")]
    public int OrdinalPosition { get; set; }

    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    [Column("COLUMN_NAME")]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type (e.g., "nvarchar", "int").
    /// </summary>
    [Column("DATA_TYPE")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is nullable ("YES" or "NO").
    /// </summary>
    [Column("IS_NULLABLE")]
    public string IsNullable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum character length.
    /// </summary>
    [Column("CHARACTER_MAXIMUM_LENGTH")]
    public int? CharacterMaximumLength { get; set; }

    /// <summary>
    /// Gets or sets the numeric precision.
    /// </summary>
    [Column("NUMERIC_PRECISION")]
    public byte? NumericPrecision { get; set; }

    /// <summary>
    /// Gets or sets the numeric scale.
    /// </summary>
    [Column("NUMERIC_SCALE")]
    public int? NumericScale { get; set; }

    /// <summary>
    /// Gets or sets the column default value.
    /// </summary>
    [Column("COLUMN_DEFAULT")]
    public string? ColumnDefault { get; set; }
}
