using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Request to compute statistical summaries grouped by one or more columns.
/// </summary>
public sealed class GroupedStatSetRequest
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container (table/view) name.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional DataStore name.</summary>
    public string? DataStoreName { get; set; }

    /// <summary>Gets or sets the optional path name.</summary>
    public string? PathName { get; set; }

    /// <summary>Gets or sets the column names to compute statistics for.</summary>
    public IReadOnlyList<string> ColumnNames { get; set; } = [];

    /// <summary>Gets or sets the column names to group by.</summary>
    public IReadOnlyList<string> GroupByColumns { get; set; } = [];

    /// <summary>
    /// Optional DataSet name. When supplied and ContainerName is empty, the endpoint resolves
    /// the DataSet to its first source's container before calling the service.
    /// </summary>
    public string? DataSetName { get; set; }
}
