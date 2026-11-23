using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One setting a component type declares that no column of the component holds (FDW-6133): a component type from another package states its own settings, and
/// each value is kept here by name as invariant text. A setting read when the step runs may name variables in it as <c>{{Name}}</c>.
/// </summary>
/// <remarks>Cascade child of the component, in <c>pipe.PipelineComponentSetting</c>, the same child-row pattern as <see cref="PipelineComponentParameterConfiguration"/>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentSettingConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the setting's name, as the component's type declares it.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as invariant text, read as the setting's data type.</summary>
    public string ValueText { get; set; } = string.Empty;
}
