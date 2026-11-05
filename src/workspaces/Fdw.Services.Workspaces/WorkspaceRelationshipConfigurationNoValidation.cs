using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Workspaces;

/// <summary>States that <see cref="WorkspaceRelationshipConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WorkspaceRelationshipConfiguration")]
public sealed class WorkspaceRelationshipConfigurationNoValidation : UnvalidatedConfiguration<WorkspaceRelationshipConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceRelationshipConfigurationNoValidation"/> class.</summary>
    public WorkspaceRelationshipConfigurationNoValidation()
        : base("WorkspaceRelationshipConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
