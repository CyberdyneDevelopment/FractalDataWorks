using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One filtered output of a conditional split (FDW-1396): the output node's name and the filter that sends a row through it.
/// </summary>
/// <remarks>Cascade child of a component, in <c>pipe.PipelineComponentSplitOutput</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentSplitOutputConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the order the outputs are tried in.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the output node's name.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the filter expression; a row that meets it goes through this output.</summary>
    public string FilterExpression { get; set; } = string.Empty;
}
