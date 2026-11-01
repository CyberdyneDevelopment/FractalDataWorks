using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Appears in search and listings, so someone can ask to join. Its contents stay closed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceVisibilities), "Discoverable")]
public sealed class DiscoverableWorkspaceVisibilityOption : WorkspaceVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscoverableWorkspaceVisibilityOption"/> class.</summary>
    public DiscoverableWorkspaceVisibilityOption() : base("Discoverable", isListed: true, grantsView: false)
    {
    }
}
