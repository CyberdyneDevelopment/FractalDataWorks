using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// One setting a component type declares that no column of the component holds (FDW-6133): a component type from another package states its own settings, and
/// each value is kept here by name as invariant text. A setting read when the step runs may name variables in it as <c>{{Name}}</c>.
/// </summary>
/// <remarks>Cascade child of a component, in <c>data.GraphDataSetComponentSetting</c>; the copy of a graph pipeline's <c>pipe.PipelineComponentSetting</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetComponentSettingConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the setting's name, as the component's type declares it.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as invariant text, read as the setting's data type.</summary>
    public string ValueText { get; set; } = string.Empty;
}
