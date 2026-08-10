using System.Collections.Generic;
using Fdw.Services.Connections.Clients.Models;

namespace Fdw.Schema.Endpoints.Ddl;

/// <summary>
/// Request to execute DDL (create a table) on a connection.
/// </summary>
public class ExecuteDdlRequest
{
    /// <summary>Gets or sets the connection name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Gets or sets the table name to create.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column definitions.</summary>
    public IList<DdlColumnRequest> Columns { get; set; } = [];
}
