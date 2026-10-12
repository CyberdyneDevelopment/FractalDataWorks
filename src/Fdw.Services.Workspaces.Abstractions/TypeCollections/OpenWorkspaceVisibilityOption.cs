using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Appears to everyone in the tenant, and its contents are readable without joining.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceVisibilities), "Open")]
public sealed class OpenWorkspaceVisibilityOption : WorkspaceVisibilityBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenWorkspaceVisibilityOption"/> class.</summary>
    public OpenWorkspaceVisibilityOption() : base("Open", isListed: true, grantsView: true)
    {
    }
}
