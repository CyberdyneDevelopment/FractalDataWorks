using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>An invitation nobody has accepted. Not membership - the row records that someone was asked, which is a different fact from their being here.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberStates), "Invited")]
public sealed class InvitedWorkspaceMemberStateOption : WorkspaceMemberStateBase
{
    /// <summary>Initializes a new instance of the <see cref="InvitedWorkspaceMemberStateOption"/> class.</summary>
    public InvitedWorkspaceMemberStateOption() : base("Invited", grantsMembership: false)
    {
    }
}
