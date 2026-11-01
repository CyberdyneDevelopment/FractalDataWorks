using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The neighbourhood of one node: what feeds it and what it feeds, out to the depth asked for.</summary>
public sealed class LineageNeighbourhoodResponse
{
    /// <summary>Gets or sets the node the answer is centred on.</summary>
    public LineageFocusDto Focus { get; set; } = new();

    /// <summary>Gets or sets the nodes, upstream first. A pipeline focus is not a node: its inputs and outputs are.</summary>
    public IReadOnlyList<LineageNodeDto> Nodes { get; set; } = [];

    /// <summary>Gets or sets the steps between the nodes sent; an edge is sent only when both its ends are.</summary>
    public IReadOnlyList<LineageEdgeDto> Edges { get; set; } = [];

    /// <summary>Gets or sets the pipelines on the edges, so a view can say how many are in view and filter by one.</summary>
    public IReadOnlyList<LineagePipelineDto> Pipelines { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the node cap cut the answer short.</summary>
    public bool Truncated { get; set; }

    /// <summary>Gets or sets the node cap the answer was held to.</summary>
    public int MaxNodes { get; set; }

    /// <summary>Gets or sets the kinds the request named that the lineage does not offer yet (Connection), so a missing kind is never silent.</summary>
    public IReadOnlyList<string> UnavailableKinds { get; set; } = [];
}
