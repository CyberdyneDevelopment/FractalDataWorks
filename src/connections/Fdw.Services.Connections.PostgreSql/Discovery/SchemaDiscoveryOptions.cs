using System.Collections.Generic;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Options for controlling PostgreSQL schema discovery behavior.
/// </summary>
/// <remarks>Every choice is required (FDW-3823): the caller that asks for the discovery states each, and nothing stands in for one.</remarks>
public sealed class SchemaDiscoveryOptions
{
    /// <summary>
    /// Gets or initializes the schemas to exclude from discovery.
    /// </summary>
    public required IReadOnlyList<string> ExcludedSchemas { get; init; }

    /// <summary>
    /// Gets or initializes the schemas to include (if specified, only these will be discovered).
    /// If null or empty, all schemas except ExcludedSchemas are included.
    /// </summary>
    public IReadOnlyList<string>? IncludeOnlySchemas { get; init; }

    /// <summary>
    /// Gets or initializes table name patterns to exclude (using SQL LIKE syntax).
    /// </summary>
    public IReadOnlyList<string>? ExcludedTablePatterns { get; init; }

    /// <summary>
    /// Gets or initializes whether to discover views in addition to tables.
    /// </summary>
    public required bool DiscoverViews { get; init; }

    /// <summary>
    /// Gets or initializes whether to discover indexes.
    /// </summary>
    public required bool DiscoverIndexes { get; init; }

    /// <summary>
    /// Gets or initializes whether to discover foreign key constraints.
    /// </summary>
    public required bool DiscoverForeignKeys { get; init; }

    /// <summary>
    /// Gets or initializes whether to discover table/column comments (pg_description).
    /// </summary>
    public required bool DiscoverDescriptions { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of tables to discover per schema.
    /// Useful for limiting discovery in large databases. 0 = no limit.
    /// </summary>
    public int MaxTablesPerSchema { get; init; }
}
