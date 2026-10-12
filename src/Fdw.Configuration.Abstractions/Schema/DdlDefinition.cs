using System.Collections.Generic;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Represents a complete DDL definition for a configuration table.
/// </summary>
public sealed class DdlDefinition
{
    /// <summary>
    /// Gets or sets the database schema name (e.g., "cfg").
    /// </summary>
    public string Schema { get; set; } = "cfg";

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = "";

    /// <summary>
    /// Gets or sets the full type name of the configuration class.
    /// </summary>
    public string ConfigurationTypeName { get; set; } = "";

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    public IList<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

    /// <summary>
    /// Gets or sets the index definitions.
    /// </summary>
    public IList<IndexDefinition> Indexes { get; set; } = new List<IndexDefinition>();

    /// <summary>
    /// Gets or sets the foreign key definitions.
    /// </summary>
    public IList<ForeignKeyDefinition> ForeignKeys { get; set; } = new List<ForeignKeyDefinition>();

    /// <summary>
    /// Gets the fully qualified table name ([schema].[tableName]).
    /// </summary>
    public string FullTableName => $"[{Schema}].[{TableName}]";
}
