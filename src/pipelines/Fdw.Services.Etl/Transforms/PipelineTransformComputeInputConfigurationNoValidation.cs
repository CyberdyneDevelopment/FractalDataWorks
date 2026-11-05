using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Transforms;

/// <summary>States that <see cref="PipelineTransformComputeInputConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineTransformComputeInputConfiguration")]
public sealed class PipelineTransformComputeInputConfigurationNoValidation : UnvalidatedConfiguration<PipelineTransformComputeInputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTransformComputeInputConfigurationNoValidation"/> class.</summary>
    public PipelineTransformComputeInputConfigurationNoValidation()
        : base("PipelineTransformComputeInputConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
