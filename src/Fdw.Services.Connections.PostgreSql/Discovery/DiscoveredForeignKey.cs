using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Represents a discovered foreign key constraint.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection for schema discovery
public sealed class DiscoveredForeignKey
{
    /// <summary>
    /// Gets the foreign key constraint name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the columns that comprise this foreign key.
    /// </summary>
    public required IReadOnlyList<DiscoveredForeignKeyColumn> Columns { get; init; }

    /// <summary>
    /// Gets the referenced schema name.
    /// </summary>
    public required string ReferencedSchema { get; init; }

    /// <summary>
    /// Gets the referenced table name.
    /// </summary>
    public required string ReferencedTable { get; init; }

    /// <summary>
    /// Gets the action to take on delete (e.g., "NO ACTION", "CASCADE", "SET NULL", "SET DEFAULT", "RESTRICT").
    /// </summary>
    public string OnDelete { get; init; } = "NO ACTION";

    /// <summary>
    /// Gets the action to take on update.
    /// </summary>
    public string OnUpdate { get; init; } = "NO ACTION";

    /// <summary>
    /// Gets the fully qualified name of the referenced table.
    /// </summary>
    public string ReferencedTableFullName => $"{ReferencedSchema}.{ReferencedTable}";
}
