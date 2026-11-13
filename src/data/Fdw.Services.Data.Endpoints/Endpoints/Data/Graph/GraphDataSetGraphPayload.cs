using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The answer of <c>GET /datasets/graph/{name}/graph</c> (FDW-5295): a Graph data set's fields out and its graph, in the shapes a request states them.</summary>
/// <remarks>
/// <see cref="Components"/> and <see cref="Links"/> are the <see cref="GraphComponentPayload"/> and <see cref="GraphLinkPayload"/> a graph pipeline states (layout is
/// the component's own), written by the pipeline's own writer, so a graph read here opens in the pipeline builder and can be sent to create, update and validate
/// unchanged; the header members (<see cref="ClassificationId"/>, <see cref="Category"/>, <see cref="Version"/>, <see cref="RecordTypeName"/>, <see cref="Caching"/>) are
/// those of the create request and are answered to every caller. When the caller may not read every source the graph reads, the graph and what names its sources are left out (null) and
/// <see cref="Restriction"/> says why: the fields and the description are always answered. A data set with no components is abstract: the lists are empty.
/// </remarks>
public sealed class GraphDataSetGraphPayload
{
    /// <summary>Gets or sets the name of the data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive the data set is (FDW-5451): the member <c>POST /datasets/graph</c> takes.</summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the name of the category the data set sits under; null when it sits under none.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the caching; null when the data set states none.</summary>
    public DataSetCachingPayload? Caching { get; set; }

    /// <summary>Gets or sets the fields out, each with its discussion and state.</summary>
    public IList<GraphFieldOutPayload> FieldsOut { get; set; } = [];

    /// <summary>Gets or sets the name of the component that carries the fields out; null while abstract, and when <see cref="Restriction"/> is stated.</summary>
    public string? FieldsOutComponent { get; set; }

    /// <summary>Gets or sets the name of the output node of <see cref="FieldsOutComponent"/> that carries the fields out; null while abstract, and when <see cref="Restriction"/> is stated.</summary>
    public string? FieldsOutOutput { get; set; }

    /// <summary>Gets or sets the components of the graph; null when <see cref="Restriction"/> is stated, empty while the data set is abstract.</summary>
    public IList<GraphComponentPayload>? Components { get; set; }

    /// <summary>Gets or sets the links of the graph; null when <see cref="Restriction"/> is stated, empty while the data set is abstract.</summary>
    public IList<GraphLinkPayload>? Links { get; set; }

    /// <summary>Gets or sets what an abstract data set intends to read; null when <see cref="Restriction"/> is stated.</summary>
    public IList<GraphIntendedSourceRequest>? IntendedSources { get; set; }

    /// <summary>Gets or sets why the graph is not answered; null when it is.</summary>
    public GraphDataSetRestrictionPayload? Restriction { get; set; }
}
