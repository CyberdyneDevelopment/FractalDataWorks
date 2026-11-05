using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A workspace owns the tag.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagOwnerKinds), "Workspace")]
public sealed class WorkspaceTagOwnerKind : TagOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceTagOwnerKind"/> class.</summary>
    public WorkspaceTagOwnerKind()
        : base("Workspace")
    {
    }
}
