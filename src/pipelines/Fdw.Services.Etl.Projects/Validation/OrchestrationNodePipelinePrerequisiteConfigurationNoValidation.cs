using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>States that <see cref="OrchestrationNodePipelinePrerequisiteConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OrchestrationNodePipelinePrerequisiteConfiguration")]
public sealed class OrchestrationNodePipelinePrerequisiteConfigurationNoValidation : UnvalidatedConfiguration<OrchestrationNodePipelinePrerequisiteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodePipelinePrerequisiteConfigurationNoValidation"/> class.</summary>
    public OrchestrationNodePipelinePrerequisiteConfigurationNoValidation()
        : base("OrchestrationNodePipelinePrerequisiteConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
