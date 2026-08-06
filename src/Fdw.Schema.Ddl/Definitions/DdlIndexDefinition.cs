#pragma warning disable CS1591
using System.Collections.Generic;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>
/// Defines a database index for DDL generation.
/// </summary>
public sealed class DdlIndexDefinition
{
    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the columns included in the index.
    /// </summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>
    /// Gets or sets whether this is a unique index.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets or sets whether this is a clustered index.
    /// </summary>
    public bool IsClustered { get; init; }

    /// <summary>
    /// Gets or sets the columns to include in the index (for covering indexes).
    /// </summary>
    public IReadOnlyList<string>? IncludeColumns { get; init; }

    /// <summary>
    /// Gets or sets the filter predicate for filtered indexes.
    /// </summary>
    public string? FilterPredicate { get; init; }

    /// <summary>
    /// Gets or sets the fill factor percentage (1-100).
    /// </summary>
    public int? FillFactor { get; init; }
}
