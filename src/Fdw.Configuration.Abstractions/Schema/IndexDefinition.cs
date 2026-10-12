namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Represents an index definition for DDL generation.
/// </summary>
public sealed class IndexDefinition
{
    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the columns included in the index.
    /// </summary>
    public string[] Columns { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether this is a unique index.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets whether this is a clustered index.
    /// </summary>
    public bool IsClustered { get; set; }

    /// <summary>
    /// Gets or sets the columns included (but not indexed) for covering.
    /// </summary>
    public string[]? IncludeColumns { get; set; }

    /// <summary>
    /// Gets or sets the filter predicate for filtered indexes.
    /// </summary>
    public string? FilterPredicate { get; set; }
}
