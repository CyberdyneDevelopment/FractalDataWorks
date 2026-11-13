using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The body of <c>PATCH /datasets/graph/{name}</c> (FDW-5287): what changes in a Graph data set; a member left out is kept.</summary>
/// <remarks>
/// A list that is sent replaces the stored one whole. <see cref="Components"/> and <see cref="Links"/> are sent together or not at all, since
/// a link names components. Binding an abstract data set sends its components and an empty <see cref="IntendedSources"/>, which replaces the intention.
/// </remarks>
public sealed class UpdateGraphDataSetRequest
{
    /// <summary>Gets or sets the name of the data set, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how sensitive the data set is.</summary>
    public int? ClassificationId { get; set; }

    /// <summary>Gets or sets the category name; empty takes the data set out of its category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }

    /// <summary>Gets or sets the fields out, replacing the stored ones.</summary>
    public IList<GraphFieldOutRequest>? FieldsOut { get; set; }

    /// <summary>Gets or sets the name of the component that carries the fields out; stated with <see cref="FieldsOutOutput"/>, left out while the data set is abstract.</summary>
    public string? FieldsOutComponent { get; set; }

    /// <summary>Gets or sets the name of the output node of <see cref="FieldsOutComponent"/> that carries the fields out.</summary>
    public string? FieldsOutOutput { get; set; }

    /// <summary>Gets or sets the components of the graph, replacing the stored ones; sent with <see cref="Links"/>.</summary>
    public IList<GraphComponentPayload>? Components { get; set; }

    /// <summary>Gets or sets the links of the graph, replacing the stored ones; sent with <see cref="Components"/>.</summary>
    public IList<GraphLinkPayload>? Links { get; set; }

    /// <summary>Gets or sets what the data set intends to read, replacing the stored intentions.</summary>
    public IList<GraphIntendedSourceRequest>? IntendedSources { get; set; }

    /// <summary>Gets or sets the caching.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
