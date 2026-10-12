using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Invisible to non-members. Only a member can find it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceVisibilities), "Private")]
public sealed class PrivateWorkspaceVisibilityOption : WorkspaceVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="PrivateWorkspaceVisibilityOption"/> class.</summary>
    public PrivateWorkspaceVisibilityOption() : base("Private", isListed: false, grantsView: false)
    {
    }
}
