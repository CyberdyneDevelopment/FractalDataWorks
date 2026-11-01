using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One link of a graph (FDW-1396): an output node of one component feeding an input node of another.
/// </summary>
/// <remarks>Cascade child of the graph, in <c>pipe.PipelineLink</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineLinkConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the producing component's name.</summary>
    public string FromComponent { get; set; } = string.Empty;

    /// <summary>Gets or sets the producing output node's name.</summary>
    public string FromOutput { get; set; } = string.Empty;

    /// <summary>Gets or sets the receiving component's name.</summary>
    public string ToComponent { get; set; } = string.Empty;

    /// <summary>Gets or sets the receiving input node's name.</summary>
    public string ToInput { get; set; } = string.Empty;
}
