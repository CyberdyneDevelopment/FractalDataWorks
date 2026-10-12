using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Owns the project. Can change anything, including who else is in it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberRoles), "Owner")]
public sealed class OwnerWorkspaceMemberRoleOption : WorkspaceMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="OwnerWorkspaceMemberRoleOption"/> class.</summary>
    public OwnerWorkspaceMemberRoleOption() : base("Owner", mayView: true, mayRead: true, mayWrite: true, mayManageMembers: true, rank: 50)
    {
    }
}
