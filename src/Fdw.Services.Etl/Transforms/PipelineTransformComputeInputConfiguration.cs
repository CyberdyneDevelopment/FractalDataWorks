using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// One input of a Compute stage: which field of the record feeds one input the Built transformation declares. Cascade child of
/// <see cref="PipelineTransformConfiguration"/> (pipe.PipelineTransformComputeInput).
/// </summary>
/// <remarks>
/// Why a row per input: the Built transformation declares what it reads by name, and the stage states, for each, where in the
/// record that value is. Nothing is matched by coincidence of names; an input with no row fails the run before any record moves.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformComputeInputConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the name of the input the Built transformation declares.</summary>
    public string InputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field of the record that feeds the input.</summary>
    public string SourceField { get; set; } = string.Empty;
}
