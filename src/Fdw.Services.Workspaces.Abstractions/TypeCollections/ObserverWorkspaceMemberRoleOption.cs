using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Looks at the project: its page, map, members and notes. Cannot query its rows or change it.</summary>
/// <remarks>
/// The per-person view-only grant (FDW-937): membership is how one user is given one workspace, and
/// every other role reads its rows. Named Observer, not Viewer, because Viewer is already a platform
/// role (authz.Role) that does reach rows, and the same word meaning the opposite would mislead.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberRoles), "Observer")]
public sealed class ObserverWorkspaceMemberRoleOption : WorkspaceMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ObserverWorkspaceMemberRoleOption"/> class.</summary>
    public ObserverWorkspaceMemberRoleOption() : base("Observer", mayView: true, mayRead: false, mayWrite: false, mayManageMembers: false, rank: 10)
    {
    }
}
