using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for the kinds of thing that can hold a dataverse membership.</summary>
/// <remarks>
/// No id is passed — it derives from the fully qualified type name. The database stores the NAME.
/// </remarks>
public abstract class DataverseSubjectTypeBase : TypeOptionBase<DataverseSubjectTypeBase>, IDataverseSubjectType
{
    /// <summary>Initializes a new instance of the <see cref="DataverseSubjectTypeBase"/> class.</summary>
    /// <param name="name">The subject-type name, which is the value persisted.</param>
    protected DataverseSubjectTypeBase(string name) : base(name)
    {
    }

    /// <summary>Whether <paramref name="subjectId"/> names this caller.</summary>
    /// <param name="subjectId">The subject recorded on the membership row.</param>
    /// <param name="callerUserId">The calling user.</param>
    /// <param name="callerRoleIds">The roles the calling user holds.</param>
    /// <remarks>
    /// On the option rather than a switch at the call site: a membership stored as a role is not
    /// expanded into a row per current member, precisely so the project's access follows the
    /// role's own membership instead of a snapshot. A caller comparing SubjectType strings would
    /// have to re-derive that, once per place that asks.
    /// </remarks>
    public abstract bool Holds(
        System.Guid subjectId,
        System.Guid callerUserId,
        System.Collections.Generic.IReadOnlyCollection<System.Guid> callerRoleIds);
}
