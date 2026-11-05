using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Workspaces;

/// <summary>States that <see cref="WorkspaceMemberConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WorkspaceMemberConfiguration")]
public sealed class WorkspaceMemberConfigurationNoValidation : UnvalidatedConfiguration<WorkspaceMemberConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMemberConfigurationNoValidation"/> class.</summary>
    public WorkspaceMemberConfigurationNoValidation()
        : base("WorkspaceMemberConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
