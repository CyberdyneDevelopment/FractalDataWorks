using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One object on the map: a data store, a pipeline, a data set, or a container an explode has brought in.</summary>
/// <remarks>
/// The id is the object's kind and logical id (<see cref="MapNodeIds"/>), so it appears once however it was reached and a
/// client merges nodes by id. There are no connection or source nodes: a data store stands for what a connection and a
/// source reach, and a data set's variations are counted on the data set rather than drawn.
/// </remarks>
public sealed class MapNodeDto
{
    /// <summary>Gets or sets the node's id: the kind of the object, a colon, and its logical id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the text the node is labelled with.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of object: DataSet, DataStore, Pipeline or Container, or another kind a workspace holds.</summary>
    /// <remarks>An open string: a client renders a kind it does not recognise as itself.</remarks>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the node's category: a data set's category, or the implementation of a data store or pipeline.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets whether the node has children an explode can bring onto the map.</summary>
    public bool CanExplode { get; set; }

    /// <summary>Gets or sets whether the caller may edit the object: the write policy of the endpoint that edits it.</summary>
    /// <remarks>The map does not edit. A screen offers to go to the workbench, or to open a panel beside the map, only when this is true.</remarks>
    public bool CanEdit { get; set; }

    /// <summary>Gets or sets the variations of this data set, counted by kind: tokens on the node, not nodes.</summary>
    /// <remarks>Empty for a node that is not a data set, and for a data set with none. One entry per kind present, in kind order.</remarks>
    public IReadOnlyList<WorkspaceMapVariationCountDto> Variations { get; set; } = [];

    /// <summary>Gets or sets the x coordinate a workspace placed the node at, or null when it was never placed.</summary>
    /// <remarks>Null rather than 0: an unplaced node must be layoutable, not stacked at the origin.</remarks>
    public decimal? X { get; set; }

    /// <summary>Gets or sets the y coordinate a workspace placed the node at, or null when it was never placed.</summary>
    public decimal? Y { get; set; }

    /// <summary>Gets or sets what else is known of the object. A value the configuration does not hold is left out.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
