using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The roles a person can hold in a workspace.</summary>
/// <remarks>
/// A closed set, like the lifecycle collections and unlike WorkspaceResourceKinds: a role describes
/// what the workspaces domain lets someone do, so a role contributed by another package would be a
/// name with no behaviour behind it. Enforced by the CHECK constraint on
/// workspace.WorkspaceMember.MemberRole; this collection is what the write path validates against, so
/// a bad value is refused by name rather than as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceMemberRoleBase), typeof(IWorkspaceMemberRole), typeof(WorkspaceMemberRoles))]
public abstract partial class WorkspaceMemberRoles : TypeCollectionBase<WorkspaceMemberRoleBase, IWorkspaceMemberRole>
{
}
