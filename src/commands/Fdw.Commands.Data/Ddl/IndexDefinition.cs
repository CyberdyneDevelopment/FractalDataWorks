using System;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Metadata for an index definition.
/// </summary>
/// <remarks>
/// <para>
/// Describes an index on one or more columns.
/// Translators convert this to backend-specific CREATE INDEX syntax.
/// </para>
/// </remarks>
public sealed class IndexDefinition
{
    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    /// <value>The name of the index.</value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the column names included in the index.
    /// </summary>
    /// <value>Array of column names in the index (order matters).</value>
    public required string[] ColumnNames { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    /// <value>True if the index enforces uniqueness; otherwise, false.</value>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is clustered.
    /// </summary>
    /// <value>True if the index is clustered; otherwise, false (non-clustered).</value>
    public bool IsClustered { get; init; }

    /// <summary>
    /// Gets or sets the included (non-key) columns for covering indexes.
    /// </summary>
    /// <value>Array of column names to include in the index leaf pages (SQL Server INCLUDE clause).</value>
    public string[] IncludeColumns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the filter condition for filtered indexes.
    /// </summary>
    /// <value>The WHERE clause for a filtered index, or null for a full index.</value>
    public string? FilterCondition { get; init; }

    /// <summary>
    /// Gets or sets the fill factor percentage.
    /// </summary>
    /// <value>The fill factor (1-100), or null for default. Lower values leave more space for growth.</value>
    public int? FillFactor { get; init; }
}
