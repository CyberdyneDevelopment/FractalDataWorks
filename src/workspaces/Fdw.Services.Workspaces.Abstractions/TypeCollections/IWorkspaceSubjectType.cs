using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>What kind of thing holds a membership — a person or a role.</summary>
public interface IWorkspaceSubjectType : ITypeOption<int, WorkspaceSubjectTypeBase>
{
    /// <summary>Whether <paramref name="subjectId"/> names this caller.</summary>
    /// <param name="subjectId">The subject recorded on the membership row.</param>
    /// <param name="callerUserId">The calling user.</param>
    /// <param name="callerRoleIds">The roles the calling user holds.</param>
    bool Holds(
        System.Guid subjectId,
        System.Guid callerUserId,
        System.Collections.Generic.IReadOnlyCollection<System.Guid> callerRoleIds);
}
