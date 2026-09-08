using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>
/// A role holds the membership, so everyone in the role is a member.
/// </summary>
/// <remarks>
/// Stored as the role rather than expanded into one row per current member, so someone joining the
/// role afterwards gets access without anyone re-granting it — and someone leaving loses it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseSubjectTypes), "Role")]
public sealed class RoleDataverseSubjectTypeOption : DataverseSubjectTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="RoleDataverseSubjectTypeOption"/> class.</summary>
    public RoleDataverseSubjectTypeOption() : base("Role")
    {
    }

    /// <inheritdoc />
    public override bool Holds(
        System.Guid subjectId,
        System.Guid callerUserId,
        System.Collections.Generic.IReadOnlyCollection<System.Guid> callerRoleIds)
        => callerRoleIds.Contains(subjectId);
}
