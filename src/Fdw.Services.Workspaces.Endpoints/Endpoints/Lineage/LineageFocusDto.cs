namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The node a lineage answer is centred on.</summary>
public sealed class LineageFocusDto
{
    /// <summary>Gets or sets the node id, kind-prefixed as on the map.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: DataSet, DataStore or Pipeline.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
}
