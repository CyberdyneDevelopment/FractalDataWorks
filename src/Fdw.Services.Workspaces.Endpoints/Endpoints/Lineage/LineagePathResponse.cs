using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>How one node reaches another, following the way data flows.</summary>
public sealed class LineagePathResponse
{
    /// <summary>Gets or sets a value indicating whether a path was found within the depth asked for.</summary>
    public bool Found { get; set; }

    /// <summary>Gets or sets the depth the search was held to.</summary>
    public int MaxDepth { get; set; }

    /// <summary>Gets or sets the nodes along the path in order, each with its step count from the start as its depth.</summary>
    public IReadOnlyList<LineageNodeDto> Nodes { get; set; } = [];

    /// <summary>Gets or sets the steps along the path.</summary>
    public IReadOnlyList<LineageEdgeDto> Edges { get; set; } = [];

    /// <summary>Gets or sets the pipelines the path passes through.</summary>
    public IReadOnlyList<LineagePipelineDto> Pipelines { get; set; } = [];
}
