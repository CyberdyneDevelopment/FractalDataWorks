using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>One single value a Compute stage hands to an input of the Built transformation it applies (FDW-1231).</summary>
/// <remarks>A cascade child of the stage, in <c>pipe.PipelineTransformComputeScalar</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformComputeScalarConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the input of the transformation that receives the value.</summary>
    public string InputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value, written as text.</summary>
    public string ValueText { get; set; } = string.Empty;

    /// <summary>Gets or sets the value's type: Number, Flag or Text.</summary>
    public string ValueType { get; set; } = string.Empty;
}
