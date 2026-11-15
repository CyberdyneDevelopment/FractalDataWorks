using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of INFORMATION_SCHEMA.KEY_COLUMN_USAGE (FDW-5876).
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaKeyColumnUsage
{
    /// <summary>Gets or sets the constraint name.</summary>
    [Column("CONSTRAINT_NAME")]
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>Gets or sets the table's schema.</summary>
    [Column("TABLE_SCHEMA")]
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>Gets or sets the table name.</summary>
    [Column("TABLE_NAME")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column name.</summary>
    [Column("COLUMN_NAME")]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position in the key.</summary>
    [Column("ORDINAL_POSITION")]
    public int OrdinalPosition { get; set; }
}
