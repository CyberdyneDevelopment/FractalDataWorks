using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Rows on either side may match many rows on the other.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceRelationshipCardinalities), "ManyToMany")]
public sealed class ManyToManyWorkspaceRelationshipCardinalityOption : WorkspaceRelationshipCardinalityBase
{
    /// <summary>Initializes a new instance of the <see cref="ManyToManyWorkspaceRelationshipCardinalityOption"/> class.</summary>
    public ManyToManyWorkspaceRelationshipCardinalityOption() : base("ManyToMany")
    {
    }
}
