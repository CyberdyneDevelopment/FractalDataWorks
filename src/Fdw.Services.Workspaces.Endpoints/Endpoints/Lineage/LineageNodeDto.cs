namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set or data store in a lineage answer.</summary>
public sealed class LineageNodeDto
{
    /// <summary>Gets or sets the node id, kind-prefixed as on the map.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: DataSet or DataStore.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace the request was scoped to, or null when it was not scoped to one.</summary>
    public string? Workspace { get; set; }

    /// <summary>Gets or sets where the node sits from the focus: negative upstream, zero at the focus, positive downstream.</summary>
    public int Depth { get; set; }
}
