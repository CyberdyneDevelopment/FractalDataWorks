using System.Collections.Generic;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents a database index on a schema entity.
/// </summary>
public sealed class SchemaIndex
{
    /// <summary>
    /// Gets or sets the name of the index.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully-qualified name of the entity this index belongs to.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered list of column names included in this index.
    /// </summary>
    public IList<string> Columns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether this index enforces uniqueness.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this index is a clustered index.
    /// </summary>
    public bool IsClustered { get; set; }

    /// <summary>
    /// Gets or sets the index type (e.g., "NONCLUSTERED", "COLUMNSTORE"), if available.
    /// </summary>
    public string? IndexType { get; set; }

    /// <summary>
    /// Gets or sets the filter predicate for a filtered index, if applicable.
    /// </summary>
    public string? FilterCondition { get; set; }
}
