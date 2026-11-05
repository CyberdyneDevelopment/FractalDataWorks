using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>States that <see cref="OrchestrationNodePipelineMembershipConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OrchestrationNodePipelineMembershipConfiguration")]
public sealed class OrchestrationNodePipelineMembershipConfigurationNoValidation : UnvalidatedConfiguration<OrchestrationNodePipelineMembershipConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodePipelineMembershipConfigurationNoValidation"/> class.</summary>
    public OrchestrationNodePipelineMembershipConfigurationNoValidation()
        : base("OrchestrationNodePipelineMembershipConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
