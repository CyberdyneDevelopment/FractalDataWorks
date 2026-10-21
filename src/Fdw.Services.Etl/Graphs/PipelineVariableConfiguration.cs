using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A variable a graph pipeline declares (FDW-1403): a named value set and read while the steps run, in scope in every step.
/// </summary>
/// <remarks>Cascade child of the graph, in <c>pipe.PipelineVariable</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineVariableConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the variable's name, unique in the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>DataTypes</c> option name of the variable's value.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the starting value as text; null starts the variable null.</summary>
    public string? ValueText { get; set; }
}
