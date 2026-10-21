using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// An output node of a graph that is deliberately not linked (FDW-1396): its rows are discarded, stated rather than assumed.
/// </summary>
/// <remarks>Cascade child of the graph, in <c>pipe.PipelineDroppedOutput</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineDroppedOutputConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the producing component's name.</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>Gets or sets the output node's name.</summary>
    public string OutputName { get; set; } = string.Empty;
}
