using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Workspaces;

/// <summary>States that <see cref="WorkspaceMembershipRequestConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WorkspaceMembershipRequestConfiguration")]
public sealed class WorkspaceMembershipRequestConfigurationNoValidation : UnvalidatedConfiguration<WorkspaceMembershipRequestConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMembershipRequestConfigurationNoValidation"/> class.</summary>
    public WorkspaceMembershipRequestConfigurationNoValidation()
        : base("WorkspaceMembershipRequestConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
