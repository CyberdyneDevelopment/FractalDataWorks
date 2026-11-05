using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineVariableConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineVariableConfiguration")]
public sealed class PipelineVariableConfigurationNoValidation : UnvalidatedConfiguration<PipelineVariableConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineVariableConfigurationNoValidation"/> class.</summary>
    public PipelineVariableConfigurationNoValidation()
        : base("PipelineVariableConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
