using System.Collections.Generic;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents a foreign key relationship between two schema entities.
/// </summary>
public sealed class SchemaRelationship
{
    /// <summary>
    /// Gets or sets the unique identifier for this relationship.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database constraint name for this relationship.
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully-qualified name of the primary (referenced) table.
    /// </summary>
    public string PrimaryTable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully-qualified name of the foreign (referencing) table.
    /// </summary>
    public string ForeignTable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column mappings between the foreign and primary tables.
    /// </summary>
    public IList<SchemaColumnMapping> ColumnMappings { get; set; } = new List<SchemaColumnMapping>();

    /// <summary>
    /// Gets or sets the cardinality type of this relationship.
    /// </summary>
    public ISchemaRelationshipType RelationshipType { get; set; } = SchemaRelationshipTypes.ManyToOne;

    /// <summary>
    /// Gets or sets the referential action applied when the primary key is updated (e.g., "CASCADE", "NO ACTION").
    /// </summary>
    public string? OnUpdate { get; set; }

    /// <summary>
    /// Gets or sets the referential action applied when the primary key is deleted (e.g., "CASCADE", "NO ACTION").
    /// </summary>
    public string? OnDelete { get; set; }

    /// <summary>
    /// Gets or sets an optional display label for this relationship in the schema diagram.
    /// </summary>
    public string? Label { get; set; }
}
