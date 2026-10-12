using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The states a workspace membership can be in.</summary>
/// <remarks>
/// Closed, for the same reason as WorkspaceMemberRoles: a state describes what the workspaces domain
/// does about a membership, so one contributed by another package would be a name with no
/// behaviour behind it. <c>workspace.WorkspaceMember.State</c> has carried a CHECK constraint over
/// exactly these four names since the table shipped — this collection is the code side of that
/// constraint, which had no code side at all, so the write path validated MemberRole and let any
/// string through as a State until the database refused it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceMemberStateBase), typeof(IWorkspaceMemberState), typeof(WorkspaceMemberStates))]
public abstract partial class WorkspaceMemberStates : TypeCollectionBase<WorkspaceMemberStateBase, IWorkspaceMemberState>
{
}
