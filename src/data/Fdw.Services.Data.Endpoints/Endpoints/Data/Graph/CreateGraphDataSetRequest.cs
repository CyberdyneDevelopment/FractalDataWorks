using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The body of <c>POST /datasets/graph</c> (FDW-5287): a Graph data set, its fields out and its graph.</summary>
/// <remarks>
/// <see cref="Components"/> and <see cref="Links"/> are the shapes a graph pipeline states (<see cref="GraphComponentPayload"/>,
/// <see cref="GraphLinkPayload"/>) and are read by the reader a graph pipeline uses, so a component's kind and settings mean what they mean there.
/// A destination, an action or a loop is refused, each by its own code. A data set with no components is abstract: its fields and its
/// <see cref="IntendedSources"/> are all it has, and reading it is refused until it is bound.
/// </remarks>
public class CreateGraphDataSetRequest
{
    /// <summary>Gets or sets the name of the data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive the data set is.</summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the name of the category the data set sits under.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the version; left out, the kind's declared one.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields out.</summary>
    public IList<GraphFieldOutRequest> FieldsOut { get; set; } = [];

    /// <summary>Gets or sets the name of the component that carries the fields out; stated with <see cref="FieldsOutOutput"/>, left out while the data set is abstract.</summary>
    public string? FieldsOutComponent { get; set; }

    /// <summary>Gets or sets the name of the output node of <see cref="FieldsOutComponent"/> that carries the fields out.</summary>
    public string? FieldsOutOutput { get; set; }

    /// <summary>Gets or sets the components of the graph.</summary>
    public IList<GraphComponentPayload> Components { get; set; } = [];

    /// <summary>Gets or sets the links of the graph.</summary>
    public IList<GraphLinkPayload> Links { get; set; } = [];

    /// <summary>Gets or sets what an abstract data set intends to read; refused when the graph has components.</summary>
    public IList<GraphIntendedSourceRequest> IntendedSources { get; set; } = [];

    /// <summary>Gets or sets the caching.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
