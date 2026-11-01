using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents a complete schema graph containing entities, relationships, and indexes
/// for a given database connection and optional schema scope.
/// </summary>
public sealed class SchemaGraph
{
    /// <summary>
    /// Gets or sets the collection of entities (tables and views) in the schema.
    /// </summary>
    public IList<SchemaEntity> Entities { get; set; } = new List<SchemaEntity>();

    /// <summary>
    /// Gets or sets the collection of foreign key relationships between entities.
    /// </summary>
    public IList<SchemaRelationship> Relationships { get; set; } = new List<SchemaRelationship>();

    /// <summary>
    /// Gets or sets the collection of indexes across all entities.
    /// </summary>
    public IList<SchemaIndex> Indexes { get; set; } = new List<SchemaIndex>();

    /// <summary>
    /// Gets or sets the name of the database connection this graph was built from.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the database, if applicable.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the schema name scope used when building this graph, if applicable.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets an empty schema graph with no entities, relationships, or indexes.
    /// </summary>
    public static SchemaGraph Empty => new();

    /// <summary>
    /// Finds an entity by its fully-qualified name.
    /// </summary>
    /// <param name="fullName">The fully-qualified entity name (e.g., "dbo.Orders").</param>
    /// <returns>The matching <see cref="SchemaEntity"/>, or <c>null</c> if not found.</returns>
    public SchemaEntity? FindEntity(string fullName) =>
        Entities.FirstOrDefault(e => string.Equals(e.FullName, fullName, System.StringComparison.Ordinal));

    /// <summary>
    /// Finds an entity by its schema and table name.
    /// </summary>
    /// <param name="schema">The schema name, or <c>null</c> to match entities with no schema.</param>
    /// <param name="tableName">The table or view name.</param>
    /// <returns>The matching <see cref="SchemaEntity"/>, or <c>null</c> if not found.</returns>
    public SchemaEntity? FindEntity(string? schema, string tableName) =>
        Entities.FirstOrDefault(e => string.Equals(e.Schema, schema, System.StringComparison.Ordinal)
                                  && string.Equals(e.TableName, tableName, System.StringComparison.Ordinal));

    /// <summary>
    /// Returns all relationships where the given entity is either the primary or foreign table.
    /// </summary>
    /// <param name="fullName">The fully-qualified entity name.</param>
    /// <returns>The relationships involving the specified entity.</returns>
    public IEnumerable<SchemaRelationship> GetRelationshipsForEntity(string fullName) =>
        Relationships.Where(r => string.Equals(r.PrimaryTable, fullName, System.StringComparison.Ordinal)
                              || string.Equals(r.ForeignTable, fullName, System.StringComparison.Ordinal));

    /// <summary>
    /// Returns the parent entities of the given entity (entities referenced by its foreign keys).
    /// </summary>
    /// <param name="fullName">The fully-qualified entity name.</param>
    /// <returns>The parent entities, with nulls filtered out.</returns>
    public IEnumerable<SchemaEntity> GetParentEntities(string fullName) =>
        Relationships.Where(r => string.Equals(r.ForeignTable, fullName, System.StringComparison.Ordinal))
                     .Select(r => FindEntity(r.PrimaryTable))
                     .Where(e => e != null)!;

    /// <summary>
    /// Returns the child entities of the given entity (entities whose foreign keys reference it).
    /// </summary>
    /// <param name="fullName">The fully-qualified entity name.</param>
    /// <returns>The child entities, with nulls filtered out.</returns>
    public IEnumerable<SchemaEntity> GetChildEntities(string fullName) =>
        Relationships.Where(r => string.Equals(r.PrimaryTable, fullName, System.StringComparison.Ordinal))
                     .Select(r => FindEntity(r.ForeignTable))
                     .Where(e => e != null)!;

    /// <summary>
    /// Returns all indexes defined on the given entity.
    /// </summary>
    /// <param name="fullName">The fully-qualified entity name.</param>
    /// <returns>The indexes for the specified entity.</returns>
    public IEnumerable<SchemaIndex> GetIndexesForEntity(string fullName) =>
        Indexes.Where(i => string.Equals(i.TableName, fullName, System.StringComparison.Ordinal));
}
