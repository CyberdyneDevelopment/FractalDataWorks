using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One level of the data option tree (FDW-6339): a node with children, or a leaf.</summary>
public sealed class DataOptionTreeNodePayload
{
    /// <summary>Gets or sets the node's name: the name a path states at this level.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the node's display name; absent where the node states none (the root), never copied from the name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets what the node is for; absent where the node states nothing.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the names below the node; empty for a leaf.</summary>
    public List<DataOptionTreeNodePayload> Children { get; set; } = [];

    /// <summary>Gets or sets what a leaf takes and the method it ends in; absent for a node with children.</summary>
    public DataOptionLeafPayload? Leaf { get; set; }
}
