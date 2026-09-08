using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The roles a person can hold in a dataverse.</summary>
/// <remarks>
/// A closed set, like the lifecycle collections and unlike DataverseResourceKinds: a role describes
/// what the dataverses domain lets someone do, so a role contributed by another package would be a
/// name with no behaviour behind it. Enforced by the CHECK constraint on
/// dataverse.DataverseMember.MemberRole; this collection is what the write path validates against, so
/// a bad value is refused by name rather than as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseMemberRoleBase), typeof(IDataverseMemberRole), typeof(DataverseMemberRoles))]
public abstract partial class DataverseMemberRoles : TypeCollectionBase<DataverseMemberRoleBase, IDataverseMemberRole>
{
}
