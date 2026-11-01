using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One link of a graph as the API states it (FDW-1396): an output node feeding an input node.</summary>
public class GraphLinkPayload
{
    /// <summary>Gets or sets the output node rows leave through.</summary>
    public GraphNodePayload From { get; set; } = new();

    /// <summary>Gets or sets the input node rows arrive at.</summary>
    public GraphNodePayload To { get; set; } = new();
}
