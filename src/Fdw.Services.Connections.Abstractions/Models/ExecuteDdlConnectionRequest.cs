using System.Collections.Generic;

namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Request DTO for executing DDL (create table) on a connection.
/// </summary>
public sealed class ExecuteDdlConnectionRequest
{
    /// <summary>Gets or sets the schema name.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Gets or sets the table name to create.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column definitions.</summary>
    public IList<DdlColumnRequest> Columns { get; set; } = new List<DdlColumnRequest>();
}
