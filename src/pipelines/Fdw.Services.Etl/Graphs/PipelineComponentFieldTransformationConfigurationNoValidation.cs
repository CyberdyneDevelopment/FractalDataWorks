using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineComponentFieldTransformationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineComponentFieldTransformationConfiguration")]
public sealed class PipelineComponentFieldTransformationConfigurationNoValidation : UnvalidatedConfiguration<PipelineComponentFieldTransformationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineComponentFieldTransformationConfigurationNoValidation"/> class.</summary>
    public PipelineComponentFieldTransformationConfigurationNoValidation()
        : base("PipelineComponentFieldTransformationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
