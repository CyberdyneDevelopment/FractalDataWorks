namespace Fdw.Services.Connections.Abstractions.Commands;

/// <summary>
/// Options for connection schema discovery.
/// </summary>
public sealed class ConnectionDiscoveryOptions
{
    /// <summary>
    /// Gets or sets whether to include metadata information.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include column information for tables.
    /// </summary>
    public bool IncludeColumns { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include relationship information.
    /// </summary>
    public bool IncludeRelationships { get; set; }

    /// <summary>
    /// Gets or sets whether to include index information.
    /// </summary>
    public bool IncludeIndexes { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth for recursive discovery.
    /// </summary>
    public int MaxDepth { get; set; } = 3;
}
