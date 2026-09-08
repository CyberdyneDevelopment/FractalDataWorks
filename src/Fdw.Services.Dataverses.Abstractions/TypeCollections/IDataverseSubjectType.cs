using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>What kind of thing holds a membership — a person or a role.</summary>
public interface IDataverseSubjectType : ITypeOption<int, DataverseSubjectTypeBase>
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
