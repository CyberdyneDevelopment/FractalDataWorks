using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// A run parameter a BatchCopy pipeline declares: a trigger or a schedule supplies a value by name, and filter
/// expressions and source queries reference it (FDW-1108). Persisted in <c>pipe.PipelineRunParameter</c>; cascade
/// child of <c>BatchCopyPipelineConfiguration</c>.
/// </summary>
/// <remarks>
/// Why no default value: a parameter is required or explicitly optional, and an absent optional parameter stays
/// absent. The reserved <c>LastSuccessfulRun</c> is never declared here; its value is stored in OpsDb
/// (<c>etl.PipelineWatermark</c>).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineRunParameterConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the parameter name the trigger or schedule supplies a value under.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the value, resolved against <see cref="DataTypes"/>: the data type the value is converted to.
    /// </summary>
    [ValuesFrom(typeof(DataTypes))]
    public string ParameterType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a run must supply the parameter.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets an optional description for the console.
    /// </summary>
    public string? Description { get; set; }
}
