using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Transforms;

/// <summary>States that <see cref="PipelineTransformComputeScalarConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineTransformComputeScalarConfiguration")]
public sealed class PipelineTransformComputeScalarConfigurationNoValidation : UnvalidatedConfiguration<PipelineTransformComputeScalarConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTransformComputeScalarConfigurationNoValidation"/> class.</summary>
    public PipelineTransformComputeScalarConfigurationNoValidation()
        : base("PipelineTransformComputeScalarConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
