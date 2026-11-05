using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Transforms;

/// <summary>States that <see cref="PipelineTransformSortFieldConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineTransformSortFieldConfiguration")]
public sealed class PipelineTransformSortFieldConfigurationNoValidation : UnvalidatedConfiguration<PipelineTransformSortFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTransformSortFieldConfigurationNoValidation"/> class.</summary>
    public PipelineTransformSortFieldConfigurationNoValidation()
        : base("PipelineTransformSortFieldConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
