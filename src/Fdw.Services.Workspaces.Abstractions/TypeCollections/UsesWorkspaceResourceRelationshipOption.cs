using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The workspace reads this resource but does not own it — it belongs to the platform or another project.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceRelationships), "Uses")]
public sealed class UsesWorkspaceResourceRelationshipOption : WorkspaceResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="UsesWorkspaceResourceRelationshipOption"/> class.</summary>
    public UsesWorkspaceResourceRelationshipOption() : base("Uses")
    {
    }
}
