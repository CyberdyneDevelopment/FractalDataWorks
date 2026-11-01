using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One resource attached to a workspace, as a node on its map.</summary>
/// <remarks>
/// Shaped to match <c>DataflowNodeDto</c> so a client renders one graph vocabulary rather than two.
///
/// There is deliberately NO binding property. Whether a resource is bound to real data is the
/// distinction the map's solid-versus-dashed treatment exists for, and nothing can currently answer
/// it: the binding columns on data.DataSetField are not exposed by any configuration class or
/// declared on any container (FDW-729). Sending an "Unknown" on every node would read as a binding
/// map where everything happens to be unknown, rather than as a platform that does not track
/// binding yet — those are different claims. The field appears when it can answer, and only for the
/// kinds that can answer it.
/// </remarks>
public class WorkspaceMapNodeDto
{
    /// <summary>Gets or sets the stable identity of the attachment, for client keys and drag state.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the text the node is labelled with.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of resource — DataSet, DataStore, Pipeline, Calculation, DataView.</summary>
    /// <remarks>
    /// An open string, not a closed set. workspace.WorkspaceResource.ResourceType carries no CHECK
    /// constraint because the kinds a host understands are the domains it referenced, so a client
    /// must render an unrecognised kind as itself rather than discarding it.
    /// </remarks>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets or sets how the workspace holds this resource — owned, or merely used.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the identity of the underlying resource this node stands for.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>Gets or sets the variations of this data set the workspace owns, counted by kind: tokens on the node, not nodes.</summary>
    /// <remarks>Empty for a node that is not a data set, and for a data set with none. One entry per kind present, in kind order (FDW-1027).</remarks>
    public IReadOnlyList<WorkspaceMapVariationCountDto> Variations { get; set; } = [];

    /// <summary>Gets or sets the x coordinate, or null when the node has never been placed.</summary>
    /// <remarks>Null rather than 0: an unplaced node must be layoutable, not stacked at the origin.</remarks>
    public decimal? X { get; set; }

    /// <summary>Gets or sets the y coordinate, or null when the node has never been placed.</summary>
    public decimal? Y { get; set; }
}
