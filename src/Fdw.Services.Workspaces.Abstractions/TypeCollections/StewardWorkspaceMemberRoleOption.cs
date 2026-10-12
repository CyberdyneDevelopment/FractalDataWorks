using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Looks after the project's data. Can edit and review, but not transfer ownership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberRoles), "Steward")]
public sealed class StewardWorkspaceMemberRoleOption : WorkspaceMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="StewardWorkspaceMemberRoleOption"/> class.</summary>
    public StewardWorkspaceMemberRoleOption() : base("Steward", mayView: true, mayRead: true, mayWrite: true, mayManageMembers: true, rank: 40)
    {
    }
}
