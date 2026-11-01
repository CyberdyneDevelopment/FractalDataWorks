using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Represents a discovered table or view.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection for schema discovery
public sealed class DiscoveredContainer
{
    /// <summary>
    /// Gets the table/view name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the container type ("Table" or "View").
    /// </summary>
    public required string ContainerType { get; init; }

    /// <summary>
    /// Gets the discovered fields (columns).
    /// </summary>
    public required IReadOnlyList<DiscoveredField> Fields { get; init; }

    /// <summary>
    /// Gets the primary key column names.
    /// </summary>
    public required IReadOnlyList<string> PrimaryKeyColumns { get; init; }

    /// <summary>
    /// Gets the discovered indexes.
    /// </summary>
    public required IReadOnlyList<DiscoveredIndex> Indexes { get; init; }

    /// <summary>
    /// Gets the discovered foreign keys.
    /// </summary>
    public IReadOnlyList<DiscoveredForeignKey> ForeignKeys { get; init; } = [];

    /// <summary>
    /// Gets the optional description/comment.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the full qualified name (schema.name).
    /// </summary>
    public string FullyQualifiedName => $"{SchemaName}.{Name}";

    /// <summary>
    /// Gets or sets the schema name (set during path creation).
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;
}
