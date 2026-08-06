using System.Collections.Generic;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Response containing the schema graph for ER diagram visualization.
/// </summary>
public class SchemaGraphResponse
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name filter applied.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Gets or sets the entities in the graph.</summary>
    public IList<SchemaGraphEntityDto> Entities { get; set; } = [];

    /// <summary>Gets or sets the relationships between entities.</summary>
    public IList<SchemaGraphRelationshipDto> Relationships { get; set; } = [];
}
