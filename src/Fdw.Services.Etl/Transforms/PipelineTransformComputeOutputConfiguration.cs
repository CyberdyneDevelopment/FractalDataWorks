using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>Where one table output of the Built transformation a Compute stage applies goes (FDW-1231).</summary>
/// <remarks>A cascade child of the stage, in <c>pipe.PipelineTransformComputeOutput</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformComputeOutputConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the table output of the transformation this row routes.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets where it goes: Replace (the rows become the stage's rows) or Named (kept under RouteName for later stages).</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the name a Named route keeps the rows under; null for Replace.</summary>
    public string? RouteName { get; set; }
}
