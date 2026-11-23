using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// An output a graph pipeline gives back to the pipeline that starts it (FDW-6136): the name the starter reads it by, the variable of the graph that holds its value when the run ends, and its data type.
/// </summary>
/// <remarks>Cascade child of the graph, in <c>pipe.PipelineOutput</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineOutputConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the output's name, unique in the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the graph's variable that holds the output's value when the run ends.</summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>DataTypes</c> option name of the output's value; the variable has to be of the same type.</summary>
    public string DataType { get; set; } = string.Empty;
}
