using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Each row on the left matches many rows on the right.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceRelationshipCardinalities), "OneToMany")]
public sealed class OneToManyWorkspaceRelationshipCardinalityOption : WorkspaceRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="OneToManyWorkspaceRelationshipCardinalityOption"/> class.</summary>
    public OneToManyWorkspaceRelationshipCardinalityOption() : base("OneToMany")
    {
    }
}
