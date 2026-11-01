namespace Fdw.Schema.Clients.Models;

using System.Collections.Generic;

/// <summary>
/// Request to create a new physical table.
/// </summary>
public sealed class CreateTableRequest
{
    /// <summary>Gets or sets the target connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the target schema name.</summary>
    public string SchemaName { get; set; } = "dbo";
    /// <summary>Gets or sets the target table name.</summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of columns to create.</summary>
    public IReadOnlyList<TableColumnRequest> Columns { get; set; } = [];
}
