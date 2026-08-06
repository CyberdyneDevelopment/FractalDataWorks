using System;
using System.Collections.Generic;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents a database entity (table or view) in a schema graph.
/// </summary>
public sealed class SchemaEntity
{
    /// <summary>
    /// Gets or sets the fully-qualified name of the entity (schema.table).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database schema name.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the table or view name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type (table or view).
    /// </summary>
    public ISchemaEntityType EntityType { get; set; } = SchemaEntityTypes.Table;

    /// <summary>
    /// Gets or sets an optional description of the entity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the collection of fields belonging to this entity.
    /// </summary>
    public IList<SchemaField> Fields { get; set; } = new List<SchemaField>();

    /// <summary>
    /// Gets or sets the approximate row count of the entity, if available.
    /// </summary>
    public long? RowCount { get; set; }

    /// <summary>
    /// Gets or sets the approximate size of the entity in kilobytes, if available.
    /// </summary>
    public long? SizeKb { get; set; }

    /// <summary>
    /// Gets or sets the visual position of this entity in the schema diagram.
    /// </summary>
    public SchemaPosition Position { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata associated with this entity.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
