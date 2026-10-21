using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One value an Execute pipeline, Execute SQL or Set variable component gives a named parameter (FDW-1403), written as text; it may
/// name a variable in double braces.
/// </summary>
/// <remarks>Cascade child of the component, in <c>pipe.PipelineComponentParameter</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentParameterConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the parameter's name, as the started pipeline declares it.</summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text.</summary>
    public string ValueText { get; set; } = string.Empty;
}
