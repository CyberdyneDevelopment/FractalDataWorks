using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Each row on the left matches at most one row on the right, and vice versa.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceRelationshipCardinalities), "OneToOne")]
public sealed class OneToOneWorkspaceRelationshipCardinalityOption : WorkspaceRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="OneToOneWorkspaceRelationshipCardinalityOption"/> class.</summary>
    public OneToOneWorkspaceRelationshipCardinalityOption() : base("OneToOne")
    {
    }
}
