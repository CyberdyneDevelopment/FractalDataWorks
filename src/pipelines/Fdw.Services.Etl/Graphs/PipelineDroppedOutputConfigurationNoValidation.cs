using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineDroppedOutputConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineDroppedOutputConfiguration")]
public sealed class PipelineDroppedOutputConfigurationNoValidation : UnvalidatedConfiguration<PipelineDroppedOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineDroppedOutputConfigurationNoValidation"/> class.</summary>
    public PipelineDroppedOutputConfigurationNoValidation()
        : base("PipelineDroppedOutputConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
