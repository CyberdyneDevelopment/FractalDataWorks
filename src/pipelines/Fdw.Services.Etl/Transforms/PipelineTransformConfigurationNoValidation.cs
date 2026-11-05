using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Transforms;

/// <summary>States that <see cref="PipelineTransformConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineTransformConfiguration")]
public sealed class PipelineTransformConfigurationNoValidation : UnvalidatedConfiguration<PipelineTransformConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTransformConfigurationNoValidation"/> class.</summary>
    public PipelineTransformConfigurationNoValidation()
        : base("PipelineTransformConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
