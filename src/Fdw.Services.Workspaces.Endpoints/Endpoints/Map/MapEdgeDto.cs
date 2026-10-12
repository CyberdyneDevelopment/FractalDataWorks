using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A relationship between two objects on the map.</summary>
/// <remarks>
/// An edge is sent once however it was derived, and only between nodes the response carries, with one exception: the
/// children of an explode draw their edges to the node that was exploded, which the client already holds.
/// </remarks>
public sealed class MapEdgeDto
{
    /// <summary>Gets or sets the edge's id, stable for the same relationship between the same two objects.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the node the edge leaves.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the node the edge reaches.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship type (<see cref="MapRelations"/>).</summary>
    public string RelationType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the caller may edit the relationship: true for a join a workspace declares when the caller may edit the workspace.</summary>
    /// <remarks>Derived edges are never editable as edges: they change when what they join is edited.</remarks>
    public bool CanEdit { get; set; }

    /// <summary>Gets or sets the label a workspace gave the relationship, when it has one.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets what else is known of the relationship.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
