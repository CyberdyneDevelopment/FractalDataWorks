using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Here, and holding whatever their role describes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberStates), "Active")]
public sealed class ActiveWorkspaceMemberStateOption : WorkspaceMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveWorkspaceMemberStateOption"/> class.</summary>
    public ActiveWorkspaceMemberStateOption() : base("Active", grantsMembership: true)
    {
    }
}
