using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Workspaces;

/// <summary>States that <see cref="WorkspaceResourceConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WorkspaceResourceConfiguration")]
public sealed class WorkspaceResourceConfigurationNoValidation : UnvalidatedConfiguration<WorkspaceResourceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceResourceConfigurationNoValidation"/> class.</summary>
    public WorkspaceResourceConfigurationNoValidation()
        : base("WorkspaceResourceConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
