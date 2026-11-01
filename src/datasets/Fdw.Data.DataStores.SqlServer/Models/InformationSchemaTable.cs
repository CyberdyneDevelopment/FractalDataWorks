namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// Represents a row from INFORMATION_SCHEMA.TABLES.
/// </summary>
public sealed class InformationSchemaTable
{
    /// <summary>
    /// Gets or sets the table schema (e.g., "dbo").
    /// </summary>
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table type (e.g., "BASE TABLE", "VIEW").
    /// </summary>
    public string TableType { get; set; } = string.Empty;
}
