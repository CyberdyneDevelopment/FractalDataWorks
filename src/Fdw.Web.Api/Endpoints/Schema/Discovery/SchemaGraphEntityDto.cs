using System.Collections.Generic;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// A single entity (table/view) in the schema graph.
/// </summary>
public class SchemaGraphEntityDto
{
    /// <summary>Gets or sets the full name (schema.table).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name.</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Gets or sets the table name.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type (Table, View).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields in the entity.</summary>
    public IList<SchemaGraphFieldDto> Fields { get; set; } = [];

    /// <summary>Gets or sets the position for layout.</summary>
    public SchemaGraphPositionDto Position { get; set; } = new();
}
