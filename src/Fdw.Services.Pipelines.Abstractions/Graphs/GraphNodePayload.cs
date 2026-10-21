using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>An input or output node of a component, as the graph names it (FDW-1396).</summary>
public class GraphNodePayload
{
    /// <summary>Gets or sets the component's name.</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>Gets or sets the node's name.</summary>
    public string Node { get; set; } = string.Empty;
}
