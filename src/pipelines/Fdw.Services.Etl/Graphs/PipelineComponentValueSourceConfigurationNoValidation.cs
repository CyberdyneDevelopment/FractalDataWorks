using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineComponentValueSourceConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineComponentValueSourceConfiguration")]
public sealed class PipelineComponentValueSourceConfigurationNoValidation : UnvalidatedConfiguration<PipelineComponentValueSourceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineComponentValueSourceConfigurationNoValidation"/> class.</summary>
    public PipelineComponentValueSourceConfigurationNoValidation()
        : base("PipelineComponentValueSourceConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
