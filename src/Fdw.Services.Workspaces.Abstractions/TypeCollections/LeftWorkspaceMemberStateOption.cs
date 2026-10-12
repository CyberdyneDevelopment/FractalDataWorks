using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Gone. Kept for the history of who was here, and grants nothing.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberStates), "Left")]
public sealed class LeftWorkspaceMemberStateOption : WorkspaceMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftWorkspaceMemberStateOption"/> class.</summary>
    public LeftWorkspaceMemberStateOption() : base("Left", grantsMembership: false)
    {
    }
}
