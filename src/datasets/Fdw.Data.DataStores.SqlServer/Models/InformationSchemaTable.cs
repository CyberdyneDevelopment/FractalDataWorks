using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// Represents a row from INFORMATION_SCHEMA.TABLES.
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaTable
{
    /// <summary>
    /// Gets or sets the table schema (e.g., "dbo").
    /// </summary>
    [Column("TABLE_SCHEMA")]
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    [Column("TABLE_NAME")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table type (e.g., "BASE TABLE", "VIEW").
    /// </summary>
    [Column("TABLE_TYPE")]
    public string TableType { get; set; } = string.Empty;
}
