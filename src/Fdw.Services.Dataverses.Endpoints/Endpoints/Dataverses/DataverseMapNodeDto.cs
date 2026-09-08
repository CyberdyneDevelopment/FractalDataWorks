using System;
using System.Collections.Generic;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>One resource attached to a dataverse, as a node on its map.</summary>
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
public class DataverseMapNodeDto
{
    /// <summary>Gets or sets the stable identity of the attachment, for client keys and drag state.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the text the node is labelled with.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of resource — DataSet, DataStore, Pipeline, Calculation, SavedView.</summary>
    /// <remarks>
    /// An open string, not a closed set. dataverse.DataverseResource.ResourceType carries no CHECK
    /// constraint because the kinds a host understands are the domains it referenced, so a client
    /// must render an unrecognised kind as itself rather than discarding it.
    /// </remarks>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets or sets how the dataverse holds this resource — owned, or merely used.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the identity of the underlying resource this node stands for.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>Gets or sets the x coordinate, or null when the node has never been placed.</summary>
    /// <remarks>Null rather than 0: an unplaced node must be layoutable, not stacked at the origin.</remarks>
    public decimal? X { get; set; }

    /// <summary>Gets or sets the y coordinate, or null when the node has never been placed.</summary>
    public decimal? Y { get; set; }
}
