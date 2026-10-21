using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A single-value setting of a component that takes its value from a run parameter instead of holding one (FDW-1722): the setting
/// by its name in the catalogue, and the parameter of the pipeline that supplies it.
/// </summary>
/// <remarks>
/// Why a row beside the component: a number or yes-or-no setting is a typed column and cannot hold the name of a parameter. The run reads the
/// parameter's value, converts it to the setting's type, and builds from a copy of the component that holds it. Cascade child of the component,
/// in <c>pipe.PipelineComponentSettingBinding</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentSettingBindingConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the setting's name in the catalogue.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline parameter the setting takes its value from.</summary>
    public string ParameterName { get; set; } = string.Empty;
}
