using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>One link of a Graph data set's graph (FDW-5287): an output node of one component feeding an input node of another.</summary>
/// <remarks>Cascade child of <see cref="GraphDataSetConfiguration"/>, in <c>data.GraphDataSetLink</c>, as a pipeline's link is in <c>pipe.PipelineLink</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetLinkConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the stable logical identifier for this link record, minted by the app before INSERT.</summary>
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
