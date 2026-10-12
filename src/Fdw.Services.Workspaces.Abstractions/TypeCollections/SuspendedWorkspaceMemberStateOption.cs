using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Still recorded, deliberately not in effect. Kept rather than deleted so restoring access does not mean reconstructing who they were.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberStates), "Suspended")]
public sealed class SuspendedWorkspaceMemberStateOption : WorkspaceMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="SuspendedWorkspaceMemberStateOption"/> class.</summary>
    public SuspendedWorkspaceMemberStateOption() : base("Suspended", grantsMembership: false)
    {
    }
}
