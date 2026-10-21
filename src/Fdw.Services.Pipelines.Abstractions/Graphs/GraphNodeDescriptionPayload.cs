using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The fields at one input or output node of a component of a graph draft (FDW-1397).</summary>
public class GraphNodeDescriptionPayload
{
    /// <summary>Gets or sets the component's name.</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>Gets or sets the node's name.</summary>
    public string Node { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the node is an input; an output when false.</summary>
    public bool IsInput { get; set; }

    /// <summary>
    /// Gets or sets the fields a row has at the node, in order; null when they cannot be known before a run (a container
    /// source, or anything fed by one). Null is not the same as an empty list, which is a node that has no fields.
    /// </summary>
    public IList<GraphFieldPayload>? Fields { get; set; }
}
