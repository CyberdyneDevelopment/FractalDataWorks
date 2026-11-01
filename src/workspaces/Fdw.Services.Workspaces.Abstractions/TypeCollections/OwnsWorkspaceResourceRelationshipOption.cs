using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The workspace owns this resource — archiving the project takes it with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceRelationships), "Owns")]
public sealed class OwnsWorkspaceResourceRelationshipOption : WorkspaceResourceRelationshipBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnsWorkspaceResourceRelationshipOption"/> class.</summary>
    public OwnsWorkspaceResourceRelationshipOption() : base("Owns")
    {
    }
}
